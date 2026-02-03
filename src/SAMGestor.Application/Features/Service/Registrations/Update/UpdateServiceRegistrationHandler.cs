using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Service.Registrations.Update;

public sealed class UpdateServiceRegistrationHandler(
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IUnitOfWork uow,
    IStorageService storage
) : IRequestHandler<UpdateServiceRegistrationCommand, UpdateServiceRegistrationResponse>
{
    public async Task<UpdateServiceRegistrationResponse> Handle(
        UpdateServiceRegistrationCommand cmd,
        CancellationToken ct)
    {
        var reg = await regRepo.GetByIdForUpdateAsync(cmd.RegistrationId, ct);
        if (reg is null)
            throw new NotFoundException(nameof(ServiceRegistration), cmd.RegistrationId);
        
        if (reg.Cpf != cmd.Cpf)
        {
            if (await regRepo.IsCpfBlockedAsync(cmd.Cpf, ct))
                throw new BusinessRuleException("CPF está bloqueado.");

            if (await regRepo.ExistsByCpfInRetreatAsync(cmd.Cpf, reg.RetreatId, ct))
                throw new BusinessRuleException("CPF já cadastrado neste retiro.");
        }

        if (reg.Email != cmd.Email)
        {
            if (await regRepo.ExistsByEmailInRetreatAsync(cmd.Email, reg.RetreatId, ct))
                throw new BusinessRuleException("E-mail já cadastrado neste retiro.");
        }

        if (cmd.PreferredSpaceId is not null)
        {
            var space = await spaceRepo.GetByIdAsync(cmd.PreferredSpaceId.Value, ct);
            if (space is null || space.RetreatId != reg.RetreatId)
                throw new BusinessRuleException("Espaço de serviço não encontrado para este retiro.");
            if (!space.IsActive)
                throw new BusinessRuleException("Espaço de serviço está inativo.");
        }

        reg.UpdateBasicInfo(
            cmd.Name,
            cmd.Cpf,
            cmd.Email,
            cmd.Phone,
            cmd.BirthDate,
            cmd.Gender,
            cmd.City
        );

        reg.SetMaritalStatus(cmd.MaritalStatus);
        reg.SetPregnancy(cmd.Pregnancy);
        reg.SetShirtSize(cmd.ShirtSize);
        reg.SetAnthropometrics(cmd.WeightKg, cmd.HeightCm);
        reg.SetProfession(cmd.Profession);
        reg.SetEducationLevel(cmd.EducationLevel);

        reg.SetAddress(
            cmd.StreetAndNumber,
            cmd.Neighborhood,
            cmd.State,
            cmd.PostalCode,
            cmd.City
        );
        reg.SetWhatsapp(cmd.Whatsapp);

        reg.SetRahaminVidaCompleted(cmd.RahaminVidaCompleted);
        reg.SetPreviousUncalledApplications(cmd.PreviousUncalledApplications);
        reg.SetPostRetreatLifeSummary(cmd.PostRetreatLifeSummary);

        reg.SetChurchLifeDescription(cmd.ChurchLifeDescription);
        reg.SetPrayerLifeDescription(cmd.PrayerLifeDescription);
        reg.SetFamilyRelationshipDescription(cmd.FamilyRelationshipDescription);
        reg.SetSelfRelationshipDescription(cmd.SelfRelationshipDescription);

        reg.UpdatePreferredSpace(cmd.PreferredSpaceId);

        if (!string.IsNullOrWhiteSpace(cmd.PhotoStorageKey))
        {
            if (!string.IsNullOrWhiteSpace(reg.PhotoStorageKey))
            {
                await storage.DeleteAsync(reg.PhotoStorageKey, ct);
            }
        
            var publicUrl = new UrlAddress(cmd.PhotoUrl!);
            reg.SetPhoto(
                cmd.PhotoStorageKey,
                cmd.PhotoContentType!,
                (int)cmd.PhotoSize!.Value,
                DateTime.UtcNow,
                publicUrl
            );
        }

        await regRepo.UpdateAsync(reg, ct);
        await uow.SaveChangesAsync(ct);

        return new UpdateServiceRegistrationResponse(
            reg.Id,
            cmd.PhotoUrl
        );
    }
}
