using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Update;

public sealed class UpdateRetreatHandler : IRequestHandler<UpdateRetreatCommand, UpdateRetreatResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateRetreatHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<UpdateRetreatResponse> Handle(
        UpdateRetreatCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.Id, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.Id);
        
        if (!retreat.IsActive())
            throw new BusinessRuleException(
                "Não é possível atualizar um retiro cancelado ou finalizado.");

        if ((string)retreat.Name != (string)cmd.Name || retreat.Edition != cmd.Edition)
        {
            var duplicated = await _repo.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, ct);
            if (duplicated)
                throw new BusinessRuleException(
                    "Já existe outro retiro com este nome e edição.");
        }
        
        retreat.UpdateDetails(
            name: cmd.Name,
            edition: cmd.Edition,
            theme: cmd.Theme,
            startDate: cmd.StartDate,
            endDate: cmd.EndDate,
            maleSlots: cmd.MaleSlots,
            femaleSlots: cmd.FemaleSlots,
            registrationStart: cmd.RegistrationStart,
            registrationEnd: cmd.RegistrationEnd,
            feeFazer: cmd.FeeFazer,
            feeServir: cmd.FeeServir,
            modifiedByUserId: cmd.ModifiedByUserId,
            shortDescription: cmd.ShortDescription,
            longDescription: cmd.LongDescription,
            location: cmd.Location,
            contactEmail: cmd.ContactEmail,
            contactPhone: cmd.ContactPhone
        );
        
        await _uow.SaveChangesAsync(ct);

        return new UpdateRetreatResponse(
            Id: retreat.Id,
            Message: "Retiro atualizado com sucesso."
        );
    }
}
