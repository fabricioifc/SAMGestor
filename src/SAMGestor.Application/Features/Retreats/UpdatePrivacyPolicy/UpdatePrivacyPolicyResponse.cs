namespace SAMGestor.Application.Features.Retreats.UpdatePrivacyPolicy;

public sealed record UpdatePrivacyPolicyResponse(
    Guid RetreatId,
    string Title,
    string Version,
    DateTime PublishedAt,
    string Message
);