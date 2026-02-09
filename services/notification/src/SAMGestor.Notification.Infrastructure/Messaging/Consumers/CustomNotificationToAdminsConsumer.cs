using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Infrastructure.Messaging.Consumers;

public sealed class CustomNotificationToAdminsConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<CustomNotificationToAdminsConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.custom.admins";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CustomNotificationToAdminsConsumer starting…");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, opt.Exchange, EventTypes.CustomNotificationToAdminsRequestedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 5, false, stoppingToken);
                logger.LogInformation("CustomNotificationToAdminsConsumer listening on {queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null)
                    {
                        await Task.Delay(400, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env = JsonSerializer.Deserialize<EventEnvelope<CustomNotificationToAdminsRequestedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing custom notification to admins. Tag={tag}", delivery.DeliveryTag);
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "CustomNotificationToAdminsConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        logger.LogInformation("CustomNotificationToAdminsConsumer stopped.");
    }

    private async Task HandleAsync(CustomNotificationToAdminsRequestedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var channels = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationChannel>>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var emailChannel = channels.Single(c => c.Name == "email");
        int successCount = 0;
        int failedCount = 0;
        string? lastError = null;

        foreach (var recipient in evt.Recipients)
        {
            try
            {
                var bodyHtml = BuildAdminNotificationTemplate(evt.Template, recipient.Name, evt.SentByName);

                var message = new NotificationMessage(
                    channel: NotificationChannel.Email,
                    recipientName: recipient.Name,
                    recipientEmail: recipient.Email,
                    recipientPhone: null,
                    templateKey: "custom-notification-admin",
                    subject: evt.Template.Subject,
                    body: bodyHtml,
                    registrationId: null, // Admin notification não tem registration
                    retreatId: null,
                    externalCorrelationId: evt.NotificationId.ToString()
                );

                await repo.AddAsync(message, ct);
                await emailChannel.SendAsync(message, ct);

                message.MarkSent();
                await repo.UpdateAsync(message, ct);
                await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);

                successCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                lastError = ex.Message;
                logger.LogError(ex,
                    "Failed to send admin notification: {NotificationId} → {Email}",
                    evt.NotificationId, recipient.Email);
            }
        }

        // Publicar resultado
        if (failedCount == 0)
        {
            await publisher.PublishAsync(
                type: EventTypes.CustomNotificationSentV1,
                source: "sam.notification",
                data: new CustomNotificationSentV1(
                    evt.NotificationId,
                    null, // Sem retreatId
                    successCount,
                    DateTimeOffset.UtcNow
                ));
        }
        else if (successCount == 0)
        {
            await publisher.PublishAsync(
                type: EventTypes.CustomNotificationFailedV1,
                source: "sam.notification",
                data: new CustomNotificationFailedV1(
                    evt.NotificationId,
                    null,
                    $"Falha total: {failedCount} erros. Último: {lastError}",
                    DateTimeOffset.UtcNow
                ));
        }
        else
        {
            await publisher.PublishAsync(
                type: EventTypes.CustomNotificationSentV1,
                source: "sam.notification",
                data: new CustomNotificationSentV1(
                    evt.NotificationId,
                    null,
                    successCount,
                    DateTimeOffset.UtcNow
                ));
        }

        logger.LogInformation(
            "Admin notification batch completed: {NotificationId}, Success={success}, Failed={failed}",
            evt.NotificationId, successCount, failedCount);
    }

    private static string BuildAdminNotificationTemplate(CustomNotificationTemplateData template, string recipientName, string sentByName)
    {
        var hasImage = !string.IsNullOrWhiteSpace(template.ImageUrl);
        var hasPreheader = !string.IsNullOrWhiteSpace(template.PreheaderText);
        var hasCTA = !string.IsNullOrWhiteSpace(template.CallToActionUrl);
        var hasSecondary = !string.IsNullOrWhiteSpace(template.SecondaryLinkUrl);

        var imageSection = hasImage
            ? $"""
              <tr>
                <td style="padding-bottom:24px;">
                  <img src="{template.ImageUrl}" alt="Banner" 
                       style="max-width:100%; height:auto; border-radius:8px; display:block;" />
                </td>
              </tr>
              """
            : "";

        var preheaderSection = hasPreheader
            ? $"""
              <tr>
                <td style="font-size:14px; color:#6b7280; padding-bottom:16px; line-height:1.5;">
                  {template.PreheaderText}
                </td>
              </tr>
              """
            : "";

        var ctaSection = hasCTA
            ? $"""
              <tr>
                <td style="padding-top:24px; padding-bottom:16px; text-align:center;">
                  <a href="{template.CallToActionUrl}" 
                     style="display:inline-block; padding:12px 32px; background-color:#2563eb; color:#ffffff; 
                            text-decoration:none; border-radius:6px; font-weight:600; font-size:16px;">
                    {template.CallToActionText ?? "Acessar"}
                  </a>
                </td>
              </tr>
              """
            : "";

        var secondarySection = hasSecondary
            ? $"""
              <tr>
                <td style="padding-top:8px; text-align:center;">
                  <a href="{template.SecondaryLinkUrl}" 
                     style="color:#2563eb; text-decoration:none; font-size:14px;">
                    {template.SecondaryLinkText ?? "Saiba mais"}
                  </a>
                </td>
              </tr>
              """
            : "";

        return $"""
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>{template.Subject}</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:32px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">

          {imageSection}

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:8px;">
              Mensagem Interna · SAMGestor
            </td>
          </tr>

          <tr>
            <td style="font-size:20px; font-weight:600; padding-bottom:8px;">
              Olá {recipientName} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:13px; color:#6b7280; padding-bottom:16px;">
              De: {sentByName}
            </td>
          </tr>

          {preheaderSection}

          <tr>
            <td style="font-size:15px; line-height:1.7; color:#374151; padding-bottom:16px;">
              {template.Body}
            </td>
          </tr>

          {ctaSection}

          {secondarySection}

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:24px; border-top:1px solid #e5e7eb;">
              Esta é uma mensagem interna do sistema SAMGestor.<br />
              <span style="color:#6b7280;">Equipe SAMGestor</span>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }
}
