using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Evento disparado quando gestor solicita envio para usuários administrativos do sistema
/// </summary>
public sealed record CustomNotificationToAdminsRequestedV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("sentByUserId")] Guid SentByUserId,
    [property: JsonPropertyName("sentByName")] string SentByName,
    [property: JsonPropertyName("sentByEmail")] string SentByEmail,
    [property: JsonPropertyName("recipients")] List<CustomNotificationRecipient> Recipients,
    [property: JsonPropertyName("template")] CustomNotificationTemplateData Template,
    [property: JsonPropertyName("requestedAt")] DateTimeOffset RequestedAt
);