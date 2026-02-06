using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

/// <summary>
/// Template de notificação personalizada com validações
/// </summary>
public sealed class NotificationTemplate : ValueObject
{
    public string Subject { get; private set; }
    public string Body { get; private set; }
    public string? PreheaderText { get; private set; }
    public string? CallToActionUrl { get; private set; }
    public string? CallToActionText { get; private set; }
    public string? SecondaryLinkUrl { get; private set; }
    public string? SecondaryLinkText { get; private set; }
    public string? ImageUrl { get; private set; }

    private NotificationTemplate() { }

    public NotificationTemplate(
        string subject,
        string body,
        string? preheaderText = null,
        string? callToActionUrl = null,
        string? callToActionText = null,
        string? secondaryLinkUrl = null,
        string? secondaryLinkText = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject é obrigatório", nameof(subject));
        
        if (subject.Length > 200)
            throw new ArgumentException("Subject não pode ter mais de 200 caracteres", nameof(subject));
        
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body é obrigatório", nameof(body));
        
        if (body.Length > 50000)
            throw new ArgumentException("Body não pode ter mais de 50.000 caracteres", nameof(body));

        Subject = subject.Trim();
        Body = body.Trim();
        PreheaderText = string.IsNullOrWhiteSpace(preheaderText) ? null : preheaderText.Trim();
        
        // Validar URLs se fornecidas
        CallToActionUrl = ValidateAndCleanUrl(callToActionUrl, nameof(callToActionUrl));
        CallToActionText = string.IsNullOrWhiteSpace(callToActionText) ? null : callToActionText.Trim();
        
        SecondaryLinkUrl = ValidateAndCleanUrl(secondaryLinkUrl, nameof(secondaryLinkUrl));
        SecondaryLinkText = string.IsNullOrWhiteSpace(secondaryLinkText) ? null : secondaryLinkText.Trim();
        
        ImageUrl = ValidateAndCleanUrl(imageUrl, nameof(imageUrl));
    }

    private static string? ValidateAndCleanUrl(string? url, string paramName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var cleaned = url.Trim();
        
        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"URL inválida: {paramName}", paramName);
        }

        return cleaned;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Subject;
        yield return Body;
        yield return PreheaderText;
        yield return CallToActionUrl;
        yield return CallToActionText;
        yield return SecondaryLinkUrl;
        yield return SecondaryLinkText;
        yield return ImageUrl;
    }
}
