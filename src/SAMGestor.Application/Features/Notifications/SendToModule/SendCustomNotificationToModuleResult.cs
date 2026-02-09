namespace SAMGestor.Application.Features.Notifications.SendToModule;

public sealed record SendCustomNotificationToModuleResult(
    Guid NotificationId,
    int TotalRecipients,
    string Status
);