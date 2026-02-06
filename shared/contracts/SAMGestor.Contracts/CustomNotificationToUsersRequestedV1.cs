using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Evento disparado quando gestor solicita envio de notificação para usuários específicos
/// </summary>
public sealed record CustomNotificationToUsersRequestedV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("retreatId")] Guid RetreatId,
    [property: JsonPropertyName("sentByUserId")] Guid SentByUserId,
    [property: JsonPropertyName("sentByName")] string SentByName,
    [property: JsonPropertyName("sentByEmail")] string SentByEmail,
    [property: JsonPropertyName("recipients")] List<CustomNotificationRecipient> Recipients,
    [property: JsonPropertyName("template")] CustomNotificationTemplateData Template,
    [property: JsonPropertyName("requestedAt")] DateTimeOffset RequestedAt
);