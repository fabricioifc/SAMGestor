using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Deactivate;

public sealed class DeactivateEmergencyCodeHandler 
    : IRequestHandler<DeactivateEmergencyCodeCommand, DeactivateEmergencyCodeResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeactivateEmergencyCodeHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DeactivateEmergencyCodeResponse> Handle(
        DeactivateEmergencyCodeCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        retreat.DeactivateEmergencyCode(cmd.Code, cmd.ModifiedByUserId);

        await _uow.SaveChangesAsync(ct);

        return new DeactivateEmergencyCodeResponse(
            RetreatId: retreat.Id,
            Code: cmd.Code,
            Message: $"Código '{cmd.Code}' desativado com sucesso. Não pode mais ser utilizado."
        );
    }
}