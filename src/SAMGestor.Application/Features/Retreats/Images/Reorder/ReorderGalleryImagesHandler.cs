using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Images.Reorder;

public sealed class ReorderGalleryImagesHandler 
    : IRequestHandler<ReorderGalleryImagesCommand, ReorderGalleryImagesResult>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public ReorderGalleryImagesHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ReorderGalleryImagesResult> Handle(
        ReorderGalleryImagesCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        var reorderList = cmd.ImageOrders
            .Select(io => (io.StorageId, io.NewOrder))
            .ToList();

        retreat.ReorderImages(reorderList, cmd.ModifiedByUserId);
        
        await _uow.SaveChangesAsync(ct);

        return new ReorderGalleryImagesResult(
            RetreatId: retreat.Id,
            ImagesReordered: cmd.ImageOrders.Count,
            Message: $"{cmd.ImageOrders.Count} imagem(ns) reordenada(s) com sucesso."
        );
    }
}