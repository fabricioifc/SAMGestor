using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.ManageStatus;

public sealed record ManageStatusResponse(
    Guid RetreatId,
    RetreatStatus Status,
    StatusAction ActionPerformed,
    string Message
);