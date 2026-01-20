using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Publish;

public sealed class PublishRetreatHandler : IRequestHandler<PublishRetreatCommand, PublishRetreatResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public PublishRetreatHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<PublishRetreatResponse> Handle(
        PublishRetreatCommand cmd,
        CancellationToken ct)
    {
        
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        if (!retreat.CanBePublished())
        {
            throw new BusinessRuleException(
                "Retiro não pode ser publicado. Verifique se todos os campos obrigatórios estão preenchidos: " +
                "Tema, Edição, Data de Início (futura), Vagas (pelo menos uma) e Política de Privacidade.");
        }
        
        if (retreat.IsPubliclyVisible)
        {
            throw new BusinessRuleException("Retiro já está publicado.");
        }
        
        retreat.Publish(cmd.ModifiedByUserId);
        
        await _uow.SaveChangesAsync(ct);

        return new PublishRetreatResponse(
            RetreatId: retreat.Id,
            IsPublished: retreat.IsPubliclyVisible,
            PublishedAt: retreat.PublishedAt,
            Message: "Retiro publicado com sucesso! Agora está visível para os participantes."
        );
    }
}