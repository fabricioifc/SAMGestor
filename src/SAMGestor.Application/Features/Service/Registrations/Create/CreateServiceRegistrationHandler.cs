using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.Create;

public sealed class CreateServiceRegistrationHandler(
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IRetreatRepository retRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateServiceRegistrationCommand, CreateServiceRegistrationResponse>
{
    public async Task<CreateServiceRegistrationResponse> Handle(
        CreateServiceRegistrationCommand cmd,
        CancellationToken ct)
    {
        var retreat = await retRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var canRegister = retreat.CanAcceptRegistrations(today, cmd.EmergencyCode);
        
        if (!canRegister)
        {
            var message = string.IsNullOrWhiteSpace(cmd.EmergencyCode)
                ? "Período de inscrições encerrado. Se você possui um código de emergência, informe-o."
                : "Código de emergência inválido, expirado ou já utilizado.";
            
            throw new BusinessRuleException(message);
        }
        
        if (await regRepo.IsCpfBlockedAsync(cmd.Cpf, ct))
            throw new BusinessRuleException("CPF está bloqueado.");

        if (await regRepo.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, ct))
            throw new BusinessRuleException("CPF já inscrito neste retiro (Servir).");

        if (await regRepo.ExistsByEmailInRetreatAsync(cmd.Email, cmd.RetreatId, ct))
            throw new BusinessRuleException("Email já inscrito neste retiro (Servir).");
        
        var hasActive = await spaceRepo.HasActiveByRetreatAsync(cmd.RetreatId, ct);
        if (hasActive && cmd.PreferredSpaceId is null)
            throw new BusinessRuleException("Espaço de serviço preferido é obrigatório.");

        if (cmd.PreferredSpaceId is not null)
        {
            var space = await spaceRepo.GetByIdAsync(cmd.PreferredSpaceId.Value, ct);
            if (space is null || space.RetreatId != cmd.RetreatId)
                throw new BusinessRuleException("Espaço de serviço não encontrado para este retiro.");
            if (!space.IsActive)
                throw new BusinessRuleException("Espaço de serviço está inativo.");
        }
        
        var entity = new ServiceRegistration(
            cmd.RetreatId, cmd.Name, cmd.Cpf, cmd.Email, cmd.Phone,
            cmd.BirthDate, cmd.Gender, cmd.City, cmd.PreferredSpaceId
        );

        await regRepo.AddAsync(entity, ct);
        
        if (!string.IsNullOrWhiteSpace(cmd.EmergencyCode))
        {
            retreat.IncrementEmergencyCodeUsage(cmd.EmergencyCode, "SYSTEM_REGISTRATION");
            await retRepo.UpdateAsync(retreat, ct);
        }

        try
        {
            await uow.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            throw new BusinessRuleException("CPF ou e-mail já inscrito neste retiro.");
        }

        return new CreateServiceRegistrationResponse(entity.Id);
    }
}
