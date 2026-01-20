using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Retreats.Images.Upload;

public sealed class UploadRetreatImageHandler 
    : IRequestHandler<UploadRetreatImageCommand, UploadRetreatImageResult>
{
    private readonly IRetreatRepository _repo;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UploadRetreatImageHandler> _logger;

    public UploadRetreatImageHandler(
        IRetreatRepository repo,
        IStorageService storage,
        IUnitOfWork uow,
        ILogger<UploadRetreatImageHandler> logger)
    {
        _repo = repo;
        _storage = storage;
        _uow = uow;
        _logger = logger;
    }

    public async Task<UploadRetreatImageResult> Handle(
        UploadRetreatImageCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var fileExtension = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        var replacedExisting = false;
        
        if (cmd.Type == ImageType.Banner || cmd.Type == ImageType.Thumbnail)
        {
            var existing = cmd.Type == ImageType.Banner 
                ? retreat.GetBanner() 
                : retreat.GetThumbnail();

            if (existing != null)
            {
                try
                {
                    await _storage.DeleteAsync(existing.StorageId, ct);
                    retreat.RemoveImage(existing.StorageId, cmd.UploadedByUserId);
                    replacedExisting = true;
                    
                    _logger.LogInformation(
                        "Imagem anterior deletada: {Type}, StorageId={StorageId}", 
                        cmd.Type, existing.StorageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Falha ao deletar imagem anterior: {StorageId}", existing.StorageId);
                }
            }
        }
        
        var typeFolder = cmd.Type.ToString().ToLowerInvariant();
        var storageKey = cmd.Type == ImageType.Gallery
            ? $"retreats/{retreat.Id}/images/gallery/{Guid.NewGuid()}{fileExtension}"
            : $"retreats/{retreat.Id}/images/{typeFolder}{fileExtension}";
        
        var (savedKey, sizeBytes) = await _storage.SaveAsync(
            cmd.FileStream,
            storageKey,
            cmd.ContentType,
            ct);
        
        var publicUrl = _storage.GetPublicUrl(savedKey);

        var retreatImage = new RetreatImage(
            imageUrl: publicUrl,
            storageId: savedKey,
            type: cmd.Type,
            order: cmd.Order,
            altText: cmd.AltText
        );

        retreat.AddImage(retreatImage, cmd.UploadedByUserId);
        
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Imagem de retiro salva: Retreat={RetreatId}, Type={Type}, Size={SizeKB}KB, StorageKey={StorageKey}",
            retreat.Id, cmd.Type, sizeBytes / 1024, savedKey);

        return new UploadRetreatImageResult(
            RetreatId: retreat.Id,
            StorageKey: savedKey,
            ImageUrl: publicUrl,
            Type: cmd.Type,
            Order: retreatImage.Order,
            UploadedAt: retreatImage.UploadedAt,
            ReplacedExisting: replacedExisting
        );
    }
}
