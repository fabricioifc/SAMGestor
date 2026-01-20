using MediatR;

namespace SAMGestor.Application.Features.Retreats.UpdatePrivacyPolicy;

public record UpdatePrivacyPolicyCommand(
    Guid RetreatId,
    string Title,
    string Body,
    string Version,
    string ModifiedByUserId
) : IRequest<UpdatePrivacyPolicyResponse>;