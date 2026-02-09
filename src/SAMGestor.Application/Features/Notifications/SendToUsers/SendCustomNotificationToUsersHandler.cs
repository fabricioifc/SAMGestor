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

namespace SAMGestor.Application.Features.Notifications.SendToUsers;

public sealed class SendCustomNotificationToUsersHandler
    : IRequestHandler<SendCustomNotificationToUsersCommand, SendCustomNotificationToUsersResult>
{
    private readonly ICustomNotificationRepository _notifications;
    private readonly IRetreatRepository _retreats;
    private readonly IRegistrationRepository _registrations;
    private readonly IServiceRegistrationRepository _serviceRegistrations;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendCustomNotificationToUsersHandler> _logger;

    public SendCustomNotificationToUsersHandler(
        ICustomNotificationRepository notifications,
        IRetreatRepository retreats,
        IRegistrationRepository registrations,
        IServiceRegistrationRepository serviceRegistrations,
        IUserRepository users,
        ICurrentUser currentUser,
        IEventBus bus,
        IUnitOfWork uow,
        ILogger<SendCustomNotificationToUsersHandler> logger)
    {
        _notifications = notifications;
        _retreats = retreats;
        _registrations = registrations;
        _serviceRegistrations = serviceRegistrations;
        _users = users;
        _currentUser = currentUser;
        _bus = bus;
        _uow = uow;
        _logger = logger;
    }

    public async Task<SendCustomNotificationToUsersResult> Handle(
        SendCustomNotificationToUsersCommand cmd,
        CancellationToken ct)
    {
        // 1. Validar autenticação
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;

        // 2. Validar permissões (Manager ou Admin)
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("manager" or "administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou enviar notificação customizada",
                currentUserId);
            throw new ForbiddenException("Apenas gestores podem enviar notificações customizadas");
        }

        // 3. Validar retiro existe
        var retreat = await _retreats.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException("Retreat", cmd.RetreatId);

        // 4. Validar UserIds não está vazio
        if (cmd.UserIds == null || !cmd.UserIds.Any())
            throw new ArgumentException("Lista de UserIds não pode estar vazia");

        // 5. Buscar destinatários via repositórios
        var allRecipients = new List<(Guid Id, string Name, string Email)>();

        foreach (var userId in cmd.UserIds)
        {
            // Tentar buscar em Registrations
            var registration = await _registrations.GetByIdAsync(userId, ct);
            if (registration != null && 
                registration.RetreatId == cmd.RetreatId && 
                registration.Enabled)
            {
                allRecipients.Add((
                    registration.Id,
                    registration.Name.Value,
                    registration.Email.Value
                ));
                continue;
            }

            // Tentar buscar em ServiceRegistrations
            var serviceReg = await _serviceRegistrations.GetByIdAsync(userId, ct);
            if (serviceReg != null && 
                serviceReg.RetreatId == cmd.RetreatId && 
                serviceReg.Enabled)
            {
                allRecipients.Add((
                    serviceReg.Id,
                    serviceReg.Name.Value,
                    serviceReg.Email.Value
                ));
            }
        }

        // Remover duplicados por email
        var uniqueRecipients = allRecipients
            .DistinctBy(r => r.Email)
            .ToList();

        if (!uniqueRecipients.Any())
            throw new InvalidOperationException("Nenhum destinatário válido encontrado");

        // 6. Criar template
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

        // 7. Criar filtro JSON
        var filterJson = JsonSerializer.Serialize(new { UserIds = cmd.UserIds });

        // 8. Criar entidade CustomNotification
        var notification = new CustomNotification(
            retreatId: cmd.RetreatId,
            sentByUserId: currentUserId,
            targetType: NotificationTargetType.SpecificUsers,
            targetFilterJson: filterJson,
            template: template,
            totalRecipients: uniqueRecipients.Count
        );

        await _notifications.AddAsync(notification, ct);

        // 9. Buscar dados do gestor
        var sender = await _users.GetByIdAsync(currentUserId, ct);
        var senderName = sender?.Name.Value ?? "Sistema";
        var senderEmail = sender?.Email.Value ?? "sistema@samgestor.com";

        // 10. Publicar evento
        var recipients = uniqueRecipients.Select(r => new CustomNotificationRecipient(
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

        var evt = new CustomNotificationToUsersRequestedV1(
            NotificationId: notification.Id,
            RetreatId: cmd.RetreatId,
            SentByUserId: currentUserId,
            SentByName: senderName,
            SentByEmail: senderEmail,
            Recipients: recipients,
            Template: templateData,
            RequestedAt: DateTimeOffset.UtcNow
        );

        await _bus.EnqueueAsync(
            type: EventTypes.CustomNotificationToUsersRequestedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // 11. Commit
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notificação customizada criada: {NotificationId}, Retiro={RetreatId}, Destinatários={Count}",
            notification.Id, cmd.RetreatId, uniqueRecipients.Count);

        return new SendCustomNotificationToUsersResult(
            NotificationId: notification.Id,
            TotalRecipients: uniqueRecipients.Count,
            Status: "Queued"
        );
    }
}
