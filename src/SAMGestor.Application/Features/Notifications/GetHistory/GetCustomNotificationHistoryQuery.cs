using MediatR;

namespace SAMGestor.Application.Features.Notifications.GetHistory;

public sealed record GetCustomNotificationHistoryQuery(
    Guid RetreatId,
    int Skip = 0,
    int Take = 50
) : IRequest<GetCustomNotificationHistoryResult>;