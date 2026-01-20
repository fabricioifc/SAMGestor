using MediatR;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.List;

public record ListEmergencyCodesQuery(
    Guid RetreatId,
    bool OnlyActive = true
) : IRequest<ListEmergencyCodesResponse>;