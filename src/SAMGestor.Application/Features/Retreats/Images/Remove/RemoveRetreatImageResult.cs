namespace SAMGestor.Application.Features.Retreats.Images.Remove;

public sealed record RemoveRetreatImageResult(
    Guid RetreatId,
    string StorageId,
    string Message
);