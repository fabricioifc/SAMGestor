namespace SAMGestor.Domain.Enums;

/// <summary>
/// Status do processamento da notificação customizada
/// </summary>
public enum CustomNotificationStatus
{
    Queued = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4
}