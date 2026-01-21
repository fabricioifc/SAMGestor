using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Registrations.Create;

public sealed class CreateRegistrationHandler(
    IRegistrationRepository regRepo,
    IRetreatRepository      retRepo,
    IUnitOfWork             uow
) : IRequestHandler<CreateRegistrationCommand, CreateRegistrationResponse>
{
    public async Task<CreateRegistrationResponse> Handle(
        CreateRegistrationCommand cmd,
        CancellationToken ct)
    {
       
        var retreat = await retRepo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);
        
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
            throw new BusinessRuleException("CPF já inscrito neste retiro.");
        
        var reg = new Registration(
            cmd.Name,
            cmd.Cpf,
            cmd.Email,
            cmd.Phone,
            cmd.BirthDate,
            cmd.Gender,
            cmd.City,
            RegistrationStatus.NotSelected,
            cmd.RetreatId
        );
        
        reg.SetMaritalStatus(cmd.MaritalStatus);
        reg.SetPregnancy(cmd.Pregnancy);
        reg.SetShirtSize(cmd.ShirtSize);
        reg.SetAnthropometrics(cmd.WeightKg, cmd.HeightCm);
        reg.SetProfession(cmd.Profession);
        reg.SetAddress(cmd.StreetAndNumber, cmd.Neighborhood, cmd.State, cmd.City);

        reg.SetWhatsapp(cmd.Whatsapp);
        reg.SetFacebookUsername(cmd.FacebookUsername);
        reg.SetInstagramHandle(cmd.InstagramHandle);
        reg.SetNeighborPhone(cmd.NeighborPhone);
        reg.SetRelativePhone(cmd.RelativePhone);

        reg.SetFather(cmd.FatherStatus, cmd.FatherName, cmd.FatherPhone);
        reg.SetMother(cmd.MotherStatus, cmd.MotherName, cmd.MotherPhone);
        reg.SetFamilyLoss(cmd.HadFamilyLossLast6Months, cmd.FamilyLossDetails);
        reg.SetSubmitterInfo(cmd.HasRelativeOrFriendSubmitted, cmd.SubmitterRelationship, cmd.SubmitterNames);

        reg.SetReligion(cmd.Religion);
        reg.SetPreviousUncalledApplications(cmd.PreviousUncalledApplications);
        reg.SetRahaminVidaCompleted(cmd.RahaminVidaCompleted);

        reg.SetAlcoholUse(cmd.AlcoholUse);
        reg.SetSmoker(cmd.Smoker);
        reg.SetDrugUse(cmd.UsesDrugs, cmd.DrugUseFrequency);
        reg.SetAllergies(cmd.HasAllergies, cmd.AllergiesDetails);
        reg.SetMedicalRestriction(cmd.HasMedicalRestriction, cmd.MedicalRestrictionDetails);
        reg.SetMedications(cmd.TakesMedication, cmd.MedicationsDetails);
        reg.SetPhysicalLimitationDetails(cmd.PhysicalLimitationDetails);
        reg.SetRecentSurgeryOrProcedureDetails(cmd.RecentSurgeryOrProcedureDetails);
        
        if (!cmd.TermsAccepted)
            throw new BusinessRuleException("Termos devem ser aceitos.");
        reg.AcceptTerms(cmd.TermsVersion, DateTime.UtcNow);
        reg.SetMarketingOptIn(cmd.MarketingOptIn ?? false, DateTime.UtcNow);
        reg.SetClientContext(cmd.ClientIp, cmd.UserAgent);
        
        if (!string.IsNullOrWhiteSpace(cmd.EmergencyCode))
        {
            retreat.IncrementEmergencyCodeUsage(cmd.EmergencyCode, "SYSTEM_REGISTRATION");
            await retRepo.UpdateAsync(retreat, ct);
        }

        await regRepo.AddAsync(reg, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateRegistrationResponse(reg.Id);
    }
}
