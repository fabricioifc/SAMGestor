using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Unpublish;

public sealed class UnpublishRetreatHandler : IRequestHandler<UnpublishRetreatCommand, UnpublishRetreatResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public UnpublishRetreatHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<UnpublishRetreatResponse> Handle(
        UnpublishRetreatCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        if (!retreat.IsPubliclyVisible)
        {
            throw new BusinessRuleException("Retiro já está despublicado (não visível).");
        }
        
        retreat.Unpublish(cmd.ModifiedByUserId);
        
        await _uow.SaveChangesAsync(ct);

        return new UnpublishRetreatResponse(
            RetreatId: retreat.Id,
            IsPublished: retreat.IsPubliclyVisible,
            Message: "Retiro despublicado com sucesso. Não está mais visível aos participantes."
        );
    }
}