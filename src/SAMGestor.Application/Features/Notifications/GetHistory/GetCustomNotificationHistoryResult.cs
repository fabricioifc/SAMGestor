namespace SAMGestor.Application.Features.Notifications.GetHistory;

public sealed record GetCustomNotificationHistoryResult(
    Guid RetreatId,
    List<CustomNotificationHistoryItem> Items,
    int Total,
    int Skip,
    int Take
);

public sealed record CustomNotificationHistoryItem(
    Guid NotificationId,
    string SentByName,
    DateTime SentAt,
    string TargetType,
    string TargetFilterJson,
    string Subject,
    int TotalRecipients,
    string Status,
    string? FailureReason
);