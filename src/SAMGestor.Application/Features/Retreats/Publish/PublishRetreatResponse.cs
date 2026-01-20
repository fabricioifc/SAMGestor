namespace SAMGestor.Application.Features.Retreats.Publish;

public sealed record PublishRetreatResponse(
    Guid RetreatId,
    bool IsPublished,
    DateTime? PublishedAt,
    string Message
);