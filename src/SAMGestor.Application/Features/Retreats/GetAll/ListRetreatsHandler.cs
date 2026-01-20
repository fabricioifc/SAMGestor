using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public sealed class ListRetreatsHandler
    : IRequestHandler<ListRetreatsQuery, PagedResult<RetreatListDto>>
{
    private readonly IRetreatRepository _repo;

    public ListRetreatsHandler(IRetreatRepository repo) => _repo = repo;

    public async Task<PagedResult<RetreatListDto>> Handle(
        ListRetreatsQuery query,
        CancellationToken ct)
    {
        var skip = Math.Max(0, query.Skip);
        var take = Math.Min(query.Take, 100); 
        
        var (retreats, totalCount) = await _repo.ListAsync(
            skip,
            take,
            query.Status,
            query.IsPubliclyVisible,
            ct
        );
        
        var items = retreats.Select(r => new RetreatListDto
        {
            Id = r.Id,
            Name = r.Name.Value,
            Edition = r.Edition,
            Theme = r.Theme,
            ShortDescription = r.ShortDescription,
            Location = r.Location,
            
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            RegistrationStart = r.RegistrationStart,
            RegistrationEnd = r.RegistrationEnd,
            
            MaleSlots = r.MaleSlots,
            FemaleSlots = r.FemaleSlots,
            TotalSlots = r.TotalSlots,
            
            FeeFazerAmount = r.FeeFazer.Amount,
            FeeServirAmount = r.FeeServir.Amount,
            
            Status = r.Status.ToString(),
            IsPubliclyVisible = r.IsPubliclyVisible,
            PublishedAt = r.PublishedAt,
            
            ThumbnailUrl = r.GetThumbnail()?.ImageUrl,
            
            CreatedAt = r.CreatedAt,
            CreatedByUserId = r.CreatedByUserId,
            LastModifiedAt = r.LastModifiedAt
        })
        .ToList();

        return new PagedResult<RetreatListDto>(items, totalCount, skip, take);
    }
}
