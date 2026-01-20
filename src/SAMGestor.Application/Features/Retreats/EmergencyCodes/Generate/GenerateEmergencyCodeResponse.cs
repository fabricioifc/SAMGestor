namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Generate;

public sealed record GenerateEmergencyCodeResponse(
    Guid RetreatId,
    string Code,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int? MaxUses,
    string Message
);