namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.List;

public sealed record ListEmergencyCodesResponse(
    Guid RetreatId,
    List<EmergencyCodeDto> Codes,
    int TotalCount,
    int ActiveCount
);

public sealed record EmergencyCodeDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public string CreatedByUserId { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public int? MaxUses { get; init; }
    public int UsedCount { get; init; }
    public bool IsExpired { get; init; }
    public bool CanBeUsed { get; init; }
}