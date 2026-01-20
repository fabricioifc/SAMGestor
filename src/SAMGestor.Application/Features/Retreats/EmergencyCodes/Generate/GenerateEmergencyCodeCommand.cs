using MediatR;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Generate;

public record GenerateEmergencyCodeCommand(
    Guid RetreatId,
    string CreatedByUserId,
    int ValidityDays = 30,
    string? Reason = null,
    int? MaxUses = null
) : IRequest<GenerateEmergencyCodeResponse>;