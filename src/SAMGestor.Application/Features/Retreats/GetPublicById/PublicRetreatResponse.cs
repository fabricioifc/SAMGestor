using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.GetPublicById;

public sealed record PublicRetreatResponse
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
    public bool IsRegistrationOpen { get; init; }
    public bool CanAcceptRegistrations { get; init; }
    
    public RetreatImageDto? Banner { get; init; }
    public RetreatImageDto? Thumbnail { get; init; }
    public List<RetreatImageDto> GalleryImages { get; init; } = new();
    
    public PrivacyPolicyDto? PrivacyPolicy { get; init; }
    public bool RequiresPrivacyPolicyAcceptance { get; init; }
}


public sealed record RetreatImageDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Order { get; init; }
    public string? AltText { get; init; }
}


public sealed record PrivacyPolicyDto
{
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTime PublishedAt { get; init; }
}
