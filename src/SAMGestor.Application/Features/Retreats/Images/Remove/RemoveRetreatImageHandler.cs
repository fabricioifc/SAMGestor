using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Images.Remove;

public sealed class RemoveRetreatImageHandler 
    : IRequestHandler<RemoveRetreatImageCommand, RemoveRetreatImageResult>
{
    private readonly IRetreatRepository _repo;
    private readonly IStorageService _storage;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RemoveRetreatImageHandler> _logger;

    public RemoveRetreatImageHandler(
        IRetreatRepository repo,
        IStorageService storage,
        IUnitOfWork uow,
        ILogger<RemoveRetreatImageHandler> logger)
    {
        _repo = repo;
        _storage = storage;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RemoveRetreatImageResult> Handle(
        RemoveRetreatImageCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        retreat.RemoveImage(cmd.StorageId, cmd.RemovedByUserId);

        try
        {
            await _storage.DeleteAsync(cmd.StorageId, ct);
            _logger.LogInformation(
                "Imagem deletada do storage: Retreat={RetreatId}, StorageId={StorageId}",
                cmd.RetreatId, cmd.StorageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao deletar arquivo do storage: {StorageId}. Continuando...",
                cmd.StorageId);
        }

        await _uow.SaveChangesAsync(ct);

        return new RemoveRetreatImageResult(
            RetreatId: retreat.Id,
            StorageId: cmd.StorageId,
            Message: "Imagem removida com sucesso."
        );
    }
}
