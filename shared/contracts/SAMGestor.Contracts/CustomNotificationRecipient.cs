using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Representa um destinatário de notificação customizada
/// </summary>
public sealed record CustomNotificationRecipient(
    [property: JsonPropertyName("id")] Guid Id, // ID do Registration, ServiceRegistration ou User
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email
);