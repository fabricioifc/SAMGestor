using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.GetById;

public record GetRetreatByIdResponse
{
   
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Edition { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? LongDescription { get; init; }
    public string? Location { get; init; }
    
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public DateOnly RegistrationStart { get; init; }
    public DateOnly RegistrationEnd { get; init; }
    
    public int MaleSlots { get; init; }
    public int FemaleSlots { get; init; }
    public int TotalSlots { get; init; }
    
    public decimal FeeFazerAmount { get; init; }
    public string FeeFazerCurrency { get; init; } = "BRL";
    public decimal FeeServirAmount { get; init; }
    public string FeeServirCurrency { get; init; } = "BRL";
    
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    
    public string Status { get; init; } = string.Empty;
    public bool IsPubliclyVisible { get; init; }
    public DateTime? PublishedAt { get; init; }
    
    public bool ContemplationClosed { get; init; }

    public int FamiliesVersion { get; init; }
    public bool FamiliesLocked { get; init; }
    
    public int ServiceSpacesVersion { get; init; }
    public bool ServiceLocked { get; init; }
    
    public int TentsVersion { get; init; }
    public bool TentsLocked { get; init; }
    
    public PrivacyPolicyDetailDto? PrivacyPolicy { get; init; }
    public bool RequiresPrivacyPolicyAcceptance { get; init; }
    
    public List<RetreatImageDetailDto> Images { get; init; } = new();
    
    public List<EmergencyCodeDetailDto> EmergencyCodes { get; init; } = new();
    public int ActiveEmergencyCodesCount { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public string CreatedByUserId { get; init; } = string.Empty;
    public DateTime? LastModifiedAt { get; init; }
    public string? LastModifiedByUserId { get; init; }
}

public record PrivacyPolicyDetailDto
{
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}

public record RetreatImageDetailDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string StorageId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Order { get; init; }
    public DateTime UploadedAt { get; init; }
    public string? AltText { get; init; }
}

public record EmergencyCodeDetailDto
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
