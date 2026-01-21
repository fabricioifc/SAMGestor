using MediatR;

namespace SAMGestor.Application.Features.Retreats.Images.Reorder;

public sealed record ReorderGalleryImagesCommand(
    Guid RetreatId,
    List<ImageOrderDto> ImageOrders,
    string ModifiedByUserId
) : IRequest<ReorderGalleryImagesResult>;


public sealed record ImageOrderDto(
    string StorageId,
    int NewOrder
);