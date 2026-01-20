using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.List;

public sealed class ListEmergencyCodesHandler 
    : IRequestHandler<ListEmergencyCodesQuery, ListEmergencyCodesResponse>
{
    private readonly IRetreatRepository _repo;

    public ListEmergencyCodesHandler(IRetreatRepository repo)
    {
        _repo = repo;
    }

    public async Task<ListEmergencyCodesResponse> Handle(
        ListEmergencyCodesQuery query,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdWithDetailsAsync(query.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), query.RetreatId);
        
        var codes = query.OnlyActive
            ? retreat.GetActiveEmergencyCodes().ToList()
            : retreat.EmergencyCodes.ToList();
        
        var codeDtos = codes.Select(code => new EmergencyCodeDto
            {
                Code = code.Code,
                CreatedAt = code.CreatedAt,
                ExpiresAt = code.ExpiresAt,
                IsActive = code.IsActive,
                CreatedByUserId = code.CreatedByUserId,
                Reason = code.Reason,
                MaxUses = code.MaxUses,
                UsedCount = code.UsedCount,
                IsExpired = code.ExpiresAt.HasValue && code.ExpiresAt.Value < DateTime.UtcNow,
                CanBeUsed = code.IsValidForUse(DateTime.UtcNow)
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        return new ListEmergencyCodesResponse(
            RetreatId: retreat.Id,
            Codes: codeDtos,
            TotalCount: codeDtos.Count,
            ActiveCount: codeDtos.Count(c => c.IsActive && c.CanBeUsed)
        );
    }
}