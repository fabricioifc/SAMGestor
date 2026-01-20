namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Deactivate;

public sealed record DeactivateEmergencyCodeResponse(
    Guid RetreatId,
    string Code,
    string Message
);