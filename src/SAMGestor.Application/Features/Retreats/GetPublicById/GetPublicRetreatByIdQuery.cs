using MediatR;

namespace SAMGestor.Application.Features.Retreats.GetPublicById;

public record GetPublicRetreatByIdQuery(Guid RetreatId) 
    : IRequest<PublicRetreatResponse>;