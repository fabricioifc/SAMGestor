using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

/// <summary>
/// Dados do template da notificação customizada
/// </summary>
public sealed record CustomNotificationTemplateData(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("preheaderText")] string? PreheaderText,
    [property: JsonPropertyName("callToActionUrl")] string? CallToActionUrl,
    [property: JsonPropertyName("callToActionText")] string? CallToActionText,
    [property: JsonPropertyName("secondaryLinkUrl")] string? SecondaryLinkUrl,
    [property: JsonPropertyName("secondaryLinkText")] string? SecondaryLinkText,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl
);