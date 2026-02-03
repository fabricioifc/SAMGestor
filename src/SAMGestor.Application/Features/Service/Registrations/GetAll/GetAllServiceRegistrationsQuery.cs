using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Registrations.GetAll;

public record GetAllServiceRegistrationsQuery(
    Guid RetreatId,
    ServiceRegistrationStatus? Status = null,
    Gender? Gender = null,
    int? MinAge = null,
    int? MaxAge = null,
    string? City = null,
    UF? State = null,
    string? Search = null,
    bool? HasPhoto = null,
    Guid? PreferredSpaceId = null, 
    bool? IsAssigned = null,       
    int Skip = 0,
    int Take = 20
) : IRequest<PagedResult<ServiceRegistrationDto>>;