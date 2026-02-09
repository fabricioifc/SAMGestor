namespace SAMGestor.Application.Features.Notifications.SendToAdmins;

public sealed record SendCustomNotificationToAdminsResult(
    Guid NotificationId,
    int TotalRecipients,
    string Status
);