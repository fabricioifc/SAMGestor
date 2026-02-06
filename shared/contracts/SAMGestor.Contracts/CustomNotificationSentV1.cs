using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Evento confirmando que notificação customizada foi enviada com sucesso
/// </summary>
public sealed record CustomNotificationSentV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("retreatId")] Guid? RetreatId,
    [property: JsonPropertyName("totalRecipients")] int TotalRecipients,
    [property: JsonPropertyName("sentAt")] DateTimeOffset SentAt
);