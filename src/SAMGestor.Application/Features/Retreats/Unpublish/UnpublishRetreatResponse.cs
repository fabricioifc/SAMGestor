namespace SAMGestor.Application.Features.Retreats.Unpublish;

public sealed record UnpublishRetreatResponse(
    Guid RetreatId,
    bool IsPublished,
    string Message
);