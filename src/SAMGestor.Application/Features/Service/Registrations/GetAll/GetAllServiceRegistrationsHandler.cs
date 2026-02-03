using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.GetAll;

public sealed class GetAllServiceRegistrationsHandler(
    IRetreatRepository retreatRepo,
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignRepo,
    IStorageService storage
) : IRequestHandler<GetAllServiceRegistrationsQuery, PagedResult<ServiceRegistrationDto>>
{
    public async Task<PagedResult<ServiceRegistrationDto>> Handle(
        GetAllServiceRegistrationsQuery query,
        CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(query.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), query.RetreatId);

        var list = await regRepo.ListByRetreatAsync(query.RetreatId, ct);

        var spaces = await spaceRepo.ListByRetreatAsync(query.RetreatId, ct);
        var spaceMap = spaces.ToDictionary(s => s.Id, s => s.Name);

        var assignments = await assignRepo.ListByRetreatAsync(query.RetreatId, ct);
        var assignmentMap = assignments.ToDictionary(a => a.ServiceRegistrationId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var filtered = list.AsEnumerable();

        if (query.Status is not null)
            filtered = filtered.Where(r => r.Status == query.Status);

        if (query.Gender is not null)
            filtered = filtered.Where(r => r.Gender == query.Gender);

        if (query.MinAge is not null)
            filtered = filtered.Where(r => r.GetAgeOn(today) >= query.MinAge);

        if (query.MaxAge is not null)
            filtered = filtered.Where(r => r.GetAgeOn(today) <= query.MaxAge);

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim().ToLowerInvariant();
            filtered = filtered.Where(r => r.City.ToLowerInvariant().Contains(city));
        }

        if (query.State is not null)
            filtered = filtered.Where(r => r.State == query.State);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            filtered = filtered.Where(r =>
                ((string)r.Name).ToLowerInvariant().Contains(s) ||
                r.Cpf.Value.Contains(s) ||
                r.Email.Value.ToLowerInvariant().Contains(s));
        }

        if (query.HasPhoto is not null)
        {
            var want = query.HasPhoto.Value;
            filtered = filtered.Where(r => 
                (r.PhotoStorageKey != null || r.PhotoUrl != null) == want);
        }

        if (query.PreferredSpaceId is not null)
            filtered = filtered.Where(r => r.PreferredSpaceId == query.PreferredSpaceId);

        if (query.IsAssigned is not null)
        {
            var want = query.IsAssigned.Value;
            filtered = filtered.Where(r => assignmentMap.ContainsKey(r.Id) == want);
        }

        var totalCount = filtered.Count();

        var ordered = filtered.OrderBy(r => (string)r.Name);

        var items = ordered
            .ApplyPagination(query.Skip, query.Take)
            .Select(r =>
            {
                var photoUrl = r.PhotoUrl?.Value;
                if (string.IsNullOrWhiteSpace(photoUrl) && !string.IsNullOrWhiteSpace(r.PhotoStorageKey))
                    photoUrl = storage.GetPublicUrl(r.PhotoStorageKey);

                string? preferredSpaceName = null;
                if (r.PreferredSpaceId is not null && spaceMap.TryGetValue(r.PreferredSpaceId.Value, out var pName))
                    preferredSpaceName = pName;

                string? assignedSpaceName = null;
                Guid? assignedSpaceId = null;
                if (assignmentMap.TryGetValue(r.Id, out var assignment))
                {
                    assignedSpaceId = assignment.ServiceSpaceId;
                    if (spaceMap.TryGetValue(assignment.ServiceSpaceId, out var aName))
                        assignedSpaceName = aName;
                }

                return new ServiceRegistrationDto(
                    r.Id,
                    (string)r.Name,
                    r.Cpf.Value,
                    r.Email.Value,
                    r.Phone,
                    r.Status.ToString(),
                    r.Gender.ToString(),
                    r.GetAgeOn(today),
                    r.City,
                    r.State?.ToString(),
                    r.RegistrationDate,
                    photoUrl,
                    r.PreferredSpaceId,
                    preferredSpaceName,
                    assignedSpaceId,
                    assignedSpaceName,
                    r.Enabled
                );
            })
            .ToList();

        return new PagedResult<ServiceRegistrationDto>(items, totalCount, query.Skip, query.Take);
    }
}
