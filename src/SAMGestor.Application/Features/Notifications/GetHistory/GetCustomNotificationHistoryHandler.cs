using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Notifications.GetHistory;

public sealed class GetCustomNotificationHistoryHandler
    : IRequestHandler<GetCustomNotificationHistoryQuery, GetCustomNotificationHistoryResult>
{
    private readonly ICustomNotificationRepository _notifications;
    private readonly IRetreatRepository _retreats;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetCustomNotificationHistoryHandler> _logger;

    public GetCustomNotificationHistoryHandler(
        ICustomNotificationRepository notifications,
        IRetreatRepository retreats,
        IUserRepository users,
        ICurrentUser currentUser,
        ILogger<GetCustomNotificationHistoryHandler> logger)
    {
        _notifications = notifications;
        _retreats = retreats;
        _users = users;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GetCustomNotificationHistoryResult> Handle(
        GetCustomNotificationHistoryQuery query,
        CancellationToken ct)
    {
        // 1. Validar autenticação
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        // 2. Validar permissões
        var userRole = _currentUser.Role?.ToLowerInvariant();
        if (userRole is not ("manager" or "administrator" or "admin"))
        {
            _logger.LogWarning(
                "Usuário {UserId} sem permissão tentou acessar histórico de notificações",
                _currentUser.UserId);
            throw new ForbiddenException("Apenas gestores podem acessar histórico de notificações");
        }

        // 3. Validar retiro existe
        var retreat = await _retreats.GetByIdAsync(query.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException("Retreat", query.RetreatId);

        // 4. Buscar notificações
        var notifications = await _notifications.ListByRetreatAsync(
            query.RetreatId,
            query.Skip,
            query.Take,
            ct
        );

        // 5. Buscar total
        var total = await _notifications.CountByRetreatAsync(query.RetreatId, ct);

        // 6. Mapear para DTOs
        var items = new List<CustomNotificationHistoryItem>();

        foreach (var notification in notifications)
        {
            // Buscar nome do gestor que enviou
            var sender = await _users.GetByIdAsync(notification.SentByUserId, ct);
            var senderName = sender?.Name.Value ?? "Usuário Removido";

            items.Add(new CustomNotificationHistoryItem(
                NotificationId: notification.Id,
                SentByName: senderName,
                SentAt: notification.SentAt,
                TargetType: notification.TargetType.ToString(),
                TargetFilterJson: notification.TargetFilterJson,
                Subject: notification.Template.Subject,
                TotalRecipients: notification.TotalRecipients,
                Status: notification.Status.ToString(),
                FailureReason: notification.FailureReason
            ));
        }

        _logger.LogInformation(
            "Histórico consultado: Retiro={RetreatId}, Total={Total}",
            query.RetreatId, total);

        return new GetCustomNotificationHistoryResult(
            RetreatId: query.RetreatId,
            Items: items,
            Total: total,
            Skip: query.Skip,
            Take: query.Take
        );
    }
}
