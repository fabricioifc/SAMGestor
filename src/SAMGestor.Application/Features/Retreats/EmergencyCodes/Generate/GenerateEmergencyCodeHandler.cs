using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Generate;

public sealed class GenerateEmergencyCodeHandler 
    : IRequestHandler<GenerateEmergencyCodeCommand, GenerateEmergencyCodeResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public GenerateEmergencyCodeHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<GenerateEmergencyCodeResponse> Handle(
        GenerateEmergencyCodeCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        if (!retreat.IsActive())
        {
            throw new BusinessRuleException(
                "Não é possível gerar código de emergência para um retiro cancelado ou finalizado.");
        }
        
        var emergencyCode = retreat.GenerateEmergencyCode(
            createdByUserId: cmd.CreatedByUserId,
            validityDays: cmd.ValidityDays,
            reason: cmd.Reason,
            maxUses: cmd.MaxUses
        );

        await _uow.SaveChangesAsync(ct);

        return new GenerateEmergencyCodeResponse(
            RetreatId: retreat.Id,
            Code: emergencyCode.Code,
            CreatedAt: emergencyCode.CreatedAt,
            ExpiresAt: emergencyCode.ExpiresAt,
            MaxUses: emergencyCode.MaxUses,
            Message: $"Código de emergência '{emergencyCode.Code}' gerado com sucesso. " +
                     $"Válido até: {(emergencyCode.ExpiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "Sem expiração")}."
        );
    }
}