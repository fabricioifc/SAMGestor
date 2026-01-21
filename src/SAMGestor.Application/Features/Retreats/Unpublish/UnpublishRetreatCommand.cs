using MediatR;

namespace SAMGestor.Application.Features.Retreats.Unpublish;

public record UnpublishRetreatCommand(
    Guid RetreatId,
    string ModifiedByUserId
) : IRequest<UnpublishRetreatResponse>;