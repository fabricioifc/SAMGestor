using MediatR;

namespace SAMGestor.Application.Features.Retreats.Images.Remove;


public sealed record RemoveRetreatImageCommand(
    Guid RetreatId,
    string StorageId,
    string RemovedByUserId
) : IRequest<RemoveRetreatImageResult>;