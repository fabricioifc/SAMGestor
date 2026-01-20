using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.ManageStatus;

public sealed class ManageStatusHandler 
    : IRequestHandler<ManageStatusCommand, ManageStatusResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public ManageStatusHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ManageStatusResponse> Handle(
        ManageStatusCommand cmd,
        CancellationToken ct)
    {
        
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var message = cmd.Action switch
        {
            StatusAction.OpenRegistration => ExecuteOpenRegistration(retreat, cmd.ModifiedByUserId),
            StatusAction.CloseRegistration => ExecuteCloseRegistration(retreat, cmd.ModifiedByUserId),
            StatusAction.Start => ExecuteStart(retreat, cmd.ModifiedByUserId),
            StatusAction.Complete => ExecuteComplete(retreat, cmd.ModifiedByUserId),
            StatusAction.Cancel => ExecuteCancel(retreat, cmd.ModifiedByUserId, cmd.Reason),
            _ => throw new BusinessRuleException("Ação de status não reconhecida.")
        };
        
        await _uow.SaveChangesAsync(ct);

        return new ManageStatusResponse(
            RetreatId: retreat.Id,
            Status: retreat.Status,
            ActionPerformed: cmd.Action,
            Message: message
        );
    }

    private static string ExecuteOpenRegistration(Retreat retreat, string userId)
    {
        retreat.OpenRegistration(userId);
        return "Inscrições abertas com sucesso! Participantes já podem se inscrever.";
    }

    private static string ExecuteCloseRegistration(Retreat retreat, string userId)
    {
        retreat.CloseRegistration(userId);
        return "Inscrições fechadas com sucesso! Novas inscrições não serão aceitas.";
    }

    private static string ExecuteStart(Retreat retreat, string userId)
    {
        retreat.Start(userId);
        return "Retiro marcado como 'Em Andamento' com sucesso!";
    }

    private static string ExecuteComplete(Retreat retreat, string userId)
    {
        retreat.Complete(userId);
        return "Retiro finalizado com sucesso!";
    }

    private static string ExecuteCancel(Retreat retreat, string userId, string? reason)
    {
        retreat.Cancel(userId);
        
        var message = "Retiro cancelado com sucesso. Não está mais visível aos participantes.";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            message += $" Motivo: {reason}";
        }
        
        return message;
    }
}
