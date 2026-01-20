using MediatR;

namespace SAMGestor.Application.Features.Retreats.ManageStatus;

public record ManageStatusCommand(
    Guid RetreatId,
    StatusAction Action,
    string ModifiedByUserId,
    string? Reason = null 
) : IRequest<ManageStatusResponse>;