namespace SAMGestor.Application.Features.Retreats.Images.Reorder;

public sealed record ReorderGalleryImagesResult(
    Guid RetreatId,
    int ImagesReordered,
    string Message
);