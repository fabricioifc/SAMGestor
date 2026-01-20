using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Services;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.Create;

public sealed class CreateRetreatHandler
    : IRequestHandler<CreateRetreatCommand, CreateRetreatResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ServiceSpacesSeeder _spacesSeeder;

    public CreateRetreatHandler(
        IRetreatRepository repo,
        IUnitOfWork uow,
        ServiceSpacesSeeder spacesSeeder)
    {
        _repo = repo;
        _uow = uow;
        _spacesSeeder = spacesSeeder;
    }

    public async Task<CreateRetreatResponse> Handle(
        CreateRetreatCommand cmd,
        CancellationToken ct)
    {
        if (await _repo.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, ct))
            throw new BusinessRuleException("Já existe um retiro com este nome e edição.");
        
        var retreat = new Retreat(
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
            createdByUserId: cmd.CreatedByUserId,
            shortDescription: cmd.ShortDescription,
            longDescription: cmd.LongDescription,
            location: cmd.Location,
            contactEmail: cmd.ContactEmail,
            contactPhone: cmd.ContactPhone
        );
        
        await _repo.AddAsync(retreat, ct);
        await _uow.SaveChangesAsync(ct);
        
        const int DefaultMaxPeople = 8;
        await _spacesSeeder.SeedDefaultsIfMissingAsync(retreat.Id, DefaultMaxPeople, ct);
        
        retreat.BumpServiceSpacesVersion();
        await _uow.SaveChangesAsync(ct);

        return new CreateRetreatResponse(
            RetreatId: retreat.Id,
            Message: "Retiro criado com sucesso. Status: Rascunho. Publique-o para torná-lo visível aos participantes."
        );
    }
}
