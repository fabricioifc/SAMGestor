using MediatR;

namespace SAMGestor.Application.Features.Retreats.Publish;

public record PublishRetreatCommand(
    Guid RetreatId,
    string ModifiedByUserId
) : IRequest<PublishRetreatResponse>;