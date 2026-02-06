using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Evento disparado quando gestor solicita envio para módulo(s) completo(s) com filtros
/// </summary>
public sealed record CustomNotificationToModuleRequestedV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("retreatId")] Guid RetreatId,
    [property: JsonPropertyName("sentByUserId")] Guid SentByUserId,
    [property: JsonPropertyName("sentByName")] string SentByName,
    [property: JsonPropertyName("sentByEmail")] string SentByEmail,
    [property: JsonPropertyName("targetModule")] string TargetModule, // "Fazer", "Servir", "Ambos"
    [property: JsonPropertyName("statusFilters")] List<string> StatusFilters, // ["Selected", "PaymentConfirmed"]
    [property: JsonPropertyName("recipients")] List<CustomNotificationRecipient> Recipients,
    [property: JsonPropertyName("template")] CustomNotificationTemplateData Template,
    [property: JsonPropertyName("requestedAt")] DateTimeOffset RequestedAt
);