using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.UpdateContact;

public sealed class UpdateContactHandler 
    : IRequestHandler<UpdateContactCommand, UpdateContactResponse>
{
    private readonly IRetreatRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateContactHandler(IRetreatRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<UpdateContactResponse> Handle(
        UpdateContactCommand cmd,
        CancellationToken ct)
    {
        var retreat = await _repo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
        retreat.UpdateContactInfo(
            email: cmd.ContactEmail,
            phone: cmd.ContactPhone,
            modifiedByUserId: cmd.ModifiedByUserId
        );

        await _uow.SaveChangesAsync(ct);

        return new UpdateContactResponse(
            RetreatId: retreat.Id,
            ContactEmail: retreat.ContactEmail,
            ContactPhone: retreat.ContactPhone,
            Message: "Informações de contato atualizadas com sucesso."
        );
    }
}