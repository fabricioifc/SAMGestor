namespace SAMGestor.Application.Features.Notifications.SendToUsers;

public sealed record SendCustomNotificationToUsersResult(
    Guid NotificationId,
    int TotalRecipients,
    string Status
);