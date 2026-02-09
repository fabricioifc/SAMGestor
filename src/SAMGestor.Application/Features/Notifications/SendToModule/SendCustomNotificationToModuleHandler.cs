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

namespace SAMGestor.Application.Features.Notifications.SendToModule;

public sealed class SendCustomNotificationToModuleHandler
    : IRequestHandler<SendCustomNotificationToModuleCommand, SendCustomNotificationToModuleResult>
{
    private readonly ICustomNotificationRepository _notifications;
    private readonly IRetreatRepository _retreats;
    private readonly IRegistrationRepository _registrations;
    private readonly IServiceRegistrationRepository _serviceRegistrations;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendCustomNotificationToModuleHandler> _logger;

    public SendCustomNotificationToModuleHandler(
        ICustomNotificationRepository notifications,
        IRetreatRepository retreats,
        IRegistrationRepository registrations,
        IServiceRegistrationRepository serviceRegistrations,
        IUserRepository users,
        ICurrentUser currentUser,
        IEventBus bus,
        IUnitOfWork uow,
        ILogger<SendCustomNotificationToModuleHandler> logger)
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

    public async Task<SendCustomNotificationToModuleResult> Handle(
        SendCustomNotificationToModuleCommand cmd,
        CancellationToken ct)
    {
        // 1. Validar autenticação
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var currentUserId = _currentUser.UserId.Value;

        // 2. Validar permissões
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("manager" or "administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou enviar notificação para módulo",
                currentUserId);
            throw new ForbiddenException("Apenas gestores podem enviar notificações");
        }

        // 3. Validar retiro
        var retreat = await _retreats.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException("Retreat", cmd.RetreatId);

        // 4. Validar TargetModule
        var validModules = new[] { "Fazer", "Servir", "Ambos" };
        if (!validModules.Contains(cmd.TargetModule, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"TargetModule inválido. Use: {string.Join(", ", validModules)}");

        // 5. Preparar filtros de status
        List<RegistrationStatus>? regStatusFilter = null;
        List<ServiceRegistrationStatus>? svcStatusFilter = null;

        if (cmd.StatusFilters != null && cmd.StatusFilters.Any())
        {
            regStatusFilter = cmd.StatusFilters
                .Where(s => Enum.TryParse<RegistrationStatus>(s, true, out _))
                .Select(s => Enum.Parse<RegistrationStatus>(s, true))
                .ToList();

            svcStatusFilter = cmd.StatusFilters
                .Where(s => Enum.TryParse<ServiceRegistrationStatus>(s, true, out _))
                .Select(s => Enum.Parse<ServiceRegistrationStatus>(s, true))
                .ToList();
        }

        // 6. Buscar destinatários via repositórios
        var allRecipients = new List<(Guid Id, string Name, string Email)>();

        // Módulo Fazer
        if (cmd.TargetModule.Equals("Fazer", StringComparison.OrdinalIgnoreCase) ||
            cmd.TargetModule.Equals("Ambos", StringComparison.OrdinalIgnoreCase))
        {
            var regRecipients = await _registrations.GetRecipientsForNotificationAsync(
                cmd.RetreatId,
                regStatusFilter,
                ct
            );
            allRecipients.AddRange(regRecipients);
        }

        // Módulo Servir
        if (cmd.TargetModule.Equals("Servir", StringComparison.OrdinalIgnoreCase) ||
            cmd.TargetModule.Equals("Ambos", StringComparison.OrdinalIgnoreCase))
        {
            var svcRecipients = await _serviceRegistrations.GetRecipientsForNotificationAsync(
                cmd.RetreatId,
                svcStatusFilter,
                ct
            );
            allRecipients.AddRange(svcRecipients);
        }

        // Remover duplicados por email
        var uniqueRecipients = allRecipients
            .DistinctBy(r => r.Email)
            .ToList();

        if (!uniqueRecipients.Any())
            throw new InvalidOperationException("Nenhum destinatário encontrado com os filtros aplicados");

        // 7. Criar template
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

        // 8. Criar filtro JSON
        var filterJson = JsonSerializer.Serialize(new
        {
            TargetModule = cmd.TargetModule,
            StatusFilters = cmd.StatusFilters ?? new List<string>()
        });

        // 9. Criar entidade
        var notification = new CustomNotification(
            retreatId: cmd.RetreatId,
            sentByUserId: currentUserId,
            targetType: NotificationTargetType.ModuleFilter,
            targetFilterJson: filterJson,
            template: template,
            totalRecipients: uniqueRecipients.Count
        );

        await _notifications.AddAsync(notification, ct);

        // 10. Buscar dados do gestor
        var sender = await _users.GetByIdAsync(currentUserId, ct);
        var senderName = sender?.Name.Value ?? "Sistema";
        var senderEmail = sender?.Email.Value ?? "sistema@samgestor.com";

        // 11. Publicar evento
        var recipientList = uniqueRecipients.Select(r => new CustomNotificationRecipient(
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

        var evt = new CustomNotificationToModuleRequestedV1(
            NotificationId: notification.Id,
            RetreatId: cmd.RetreatId,
            SentByUserId: currentUserId,
            SentByName: senderName,
            SentByEmail: senderEmail,
            TargetModule: cmd.TargetModule,
            StatusFilters: cmd.StatusFilters ?? new List<string>(),
            Recipients: recipientList,
            Template: templateData,
            RequestedAt: DateTimeOffset.UtcNow
        );

        await _bus.EnqueueAsync(
            type: EventTypes.CustomNotificationToModuleRequestedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // 12. Commit
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notificação para módulo criada: {NotificationId}, Retiro={RetreatId}, Módulo={Module}, Destinatários={Count}",
            notification.Id, cmd.RetreatId, cmd.TargetModule, uniqueRecipients.Count);

        return new SendCustomNotificationToModuleResult(
            NotificationId: notification.Id,
            TotalRecipients: uniqueRecipients.Count,
            Status: "Queued"
        );
    }
}
