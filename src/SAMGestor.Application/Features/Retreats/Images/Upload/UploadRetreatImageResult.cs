using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Retreats.Images.Upload;

public sealed record UploadRetreatImageResult(
    Guid RetreatId,
    string StorageKey,
    string ImageUrl,
    ImageType Type,
    int Order,
    DateTime UploadedAt,
    bool ReplacedExisting
);