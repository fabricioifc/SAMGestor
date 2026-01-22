using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Delete;

public sealed class DeleteRetreatHandler : IRequestHandler<DeleteRetreatCommand, DeleteRetreatResponse>
{
    private readonly IRetreatRepository _retreatRepo;
    private readonly IRegistrationRepository _registrationRepo;
    private readonly IServiceRegistrationRepository _serviceRegistrationRepo;
    private readonly IUnitOfWork _uow;

    public DeleteRetreatHandler(
        IRetreatRepository retreatRepo,
        IRegistrationRepository registrationRepo,
        IServiceRegistrationRepository serviceRegistrationRepo,
        IUnitOfWork uow)
    {
        _retreatRepo = retreatRepo;
        _registrationRepo = registrationRepo;
        _serviceRegistrationRepo = serviceRegistrationRepo;
        _uow = uow;
    }

    public async Task<DeleteRetreatResponse> Handle(
        DeleteRetreatCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _retreatRepo.GetByIdAsync(cmd.Id, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.Id);
        
        var fazerCount = await _registrationRepo.CountByRetreatAsync(cmd.Id, ct);
        
        var servirCount = await _serviceRegistrationRepo.CountByRetreatAsync(cmd.Id, ct);
        
        var totalRegistrations = fazerCount + servirCount;

        if (totalRegistrations > 0)
        {
            throw new BusinessRuleException(
                $"Não é possível excluir o retiro porque existem {totalRegistrations} inscrição(ões) vinculada(s). " +
                $"({fazerCount} Fazer, {servirCount} Servir). " +
                $"Para remover o retiro do sistema, cancele-o ao invés de excluí-lo.");
        }

        await _retreatRepo.RemoveAsync(retreat, ct);
        await _uow.SaveChangesAsync(ct);

        return new DeleteRetreatResponse(cmd.Id);
    }
}
