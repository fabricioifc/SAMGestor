using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Text.Json;

namespace SAMGestor.Application.Features.Notifications.SendToAdmins;

public sealed class SendCustomNotificationToAdminsHandler
    : IRequestHandler<SendCustomNotificationToAdminsCommand, SendCustomNotificationToAdminsResult>
{
    private readonly ICustomNotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendCustomNotificationToAdminsHandler> _logger;

    public SendCustomNotificationToAdminsHandler(
        ICustomNotificationRepository notifications,
        IUserRepository users,
        ICurrentUser currentUser,
        IEventBus bus,
        IUnitOfWork uow,
        ILogger<SendCustomNotificationToAdminsHandler> logger)
    {
        _notifications = notifications;
        _users = users;
        _currentUser = currentUser;
        _bus = bus;
        _uow = uow;
        _logger = logger;
    }

    public async Task<SendCustomNotificationToAdminsResult> Handle(
        SendCustomNotificationToAdminsCommand cmd,
        CancellationToken ct)
    {
        // 1. Validar autenticação
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;

        // 2. Validar permissões (apenas Admin pode notificar outros admins)
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou enviar notificação para administradores",
                currentUserId);
            throw new ForbiddenException("Apenas administradores podem enviar notificações para usuários do sistema");
        }

        // 3. Buscar destinatários
        List<(Guid Id, string Name, string Email)> recipients;

        if (cmd.UserIds != null && cmd.UserIds.Any())
        {
            // Enviar para usuários específicos
            recipients = new List<(Guid Id, string Name, string Email)>();
            
            foreach (var userId in cmd.UserIds)
            {
                var user = await _users.GetByIdAsync(userId, ct);
                if (user != null && user.Enabled)
                {
                    recipients.Add((
                        user.Id,
                        user.Name.Value,
                        user.Email.Value
                    ));
                }
            }
        }
        else
        {
            // Enviar para todos os admins/gestores/consultores
            var adminRoles = new[] { UserRole.Administrator, UserRole.Manager, UserRole.Consultant };
            recipients = await _users.GetUsersByRolesAsync(adminRoles, ct);
        }

        if (!recipients.Any())
            throw new InvalidOperationException("Nenhum destinatário válido encontrado");

        // 4. Criar template
        var template = new NotificationTemplate(
            subject: cmd.Subject,
            body: cmd.Body,
            preheaderText: cmd.PreheaderText,
            callToActionUrl: cmd.CallToActionUrl,
            callToActionText: cmd.CallToActionText,
            secondaryLinkUrl: cmd.SecondaryLinkUrl,
            secondaryLinkText: cmd.SecondaryLinkText,
            imageUrl: cmd.ImageUrl
        );

        // 5. Criar filtro JSON
        var filterJson = cmd.UserIds != null && cmd.UserIds.Any()
            ? JsonSerializer.Serialize(new { UserIds = cmd.UserIds })
            : JsonSerializer.Serialize(new { Target = "AllAdmins" });

        // 6. Criar entidade CustomNotification (sem RetreatId)
        var notification = new CustomNotification(
            retreatId: null, // Notificação administrativa não está ligada a retiro
            sentByUserId: currentUserId,
            targetType: NotificationTargetType.AdminUsers,
            targetFilterJson: filterJson,
            template: template,
            totalRecipients: recipients.Count
        );

        await _notifications.AddAsync(notification, ct);

        // 7. Buscar dados do gestor
        var sender = await _users.GetByIdAsync(currentUserId, ct);
        var senderName = sender?.Name.Value ?? "Sistema";
        var senderEmail = sender?.Email.Value ?? "sistema@samgestor.com";

        // 8. Publicar evento
        var recipientList = recipients.Select(r => new CustomNotificationRecipient(
            Id: r.Id,
            Name: r.Name,
            Email: r.Email
        )).ToList();

        var templateData = new CustomNotificationTemplateData(
            Subject: template.Subject,
            Body: template.Body,
            PreheaderText: template.PreheaderText,
            CallToActionUrl: template.CallToActionUrl,
            CallToActionText: template.CallToActionText,
            SecondaryLinkUrl: template.SecondaryLinkUrl,
            SecondaryLinkText: template.SecondaryLinkText,
            ImageUrl: template.ImageUrl
        );

        var evt = new CustomNotificationToAdminsRequestedV1(
            NotificationId: notification.Id,
            SentByUserId: currentUserId,
            SentByName: senderName,
            SentByEmail: senderEmail,
            Recipients: recipientList,
            Template: templateData,
            RequestedAt: DateTimeOffset.UtcNow
        );

        await _bus.EnqueueAsync(
            type: EventTypes.CustomNotificationToAdminsRequestedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // 9. Commit
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notificação para admins criada: {NotificationId}, Destinatários={Count}",
            notification.Id, recipients.Count);

        return new SendCustomNotificationToAdminsResult(
            NotificationId: notification.Id,
            TotalRecipients: recipients.Count,
            Status: "Queued"
        );
    }
}
