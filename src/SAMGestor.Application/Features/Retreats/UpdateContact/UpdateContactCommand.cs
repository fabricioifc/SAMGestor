using MediatR;

namespace SAMGestor.Application.Features.Retreats.UpdateContact;

public record UpdateContactCommand(
    Guid RetreatId,
    string ModifiedByUserId,
    string? ContactEmail = null,
    string? ContactPhone = null
) : IRequest<UpdateContactResponse>;