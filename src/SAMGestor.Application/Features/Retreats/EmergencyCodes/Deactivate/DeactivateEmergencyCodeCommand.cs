using MediatR;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Deactivate;

public record DeactivateEmergencyCodeCommand(
    Guid RetreatId,
    string Code,
    string ModifiedByUserId
) : IRequest<DeactivateEmergencyCodeResponse>;