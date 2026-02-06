using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Evento indicando falha no envio de notificação customizada
/// </summary>
public sealed record CustomNotificationFailedV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("retreatId")] Guid? RetreatId,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("failedAt")] DateTimeOffset FailedAt
);