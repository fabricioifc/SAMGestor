using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public record ListRetreatsQuery(
    int Skip = 0,
    int Take = 20,
    RetreatStatus? Status = null,
    bool? IsPubliclyVisible = null
) : IRequest<PagedResult<RetreatListDto>>;

public record RetreatListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Edition { get; init; } = string.Empty;
    public string Theme { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Location { get; init; }
    
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public DateOnly RegistrationStart { get; init; }
    public DateOnly RegistrationEnd { get; init; }
    
    public int MaleSlots { get; init; }
    public int FemaleSlots { get; init; }
    public int TotalSlots { get; init; }
    
    public decimal FeeFazerAmount { get; init; }
    public decimal FeeServirAmount { get; init; }
    
    public string Status { get; init; } = string.Empty;
    public bool IsPubliclyVisible { get; init; }
    public DateTime? PublishedAt { get; init; }
    
    public string? ThumbnailUrl { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public string CreatedByUserId { get; init; } = string.Empty;
    public DateTime? LastModifiedAt { get; init; }
}