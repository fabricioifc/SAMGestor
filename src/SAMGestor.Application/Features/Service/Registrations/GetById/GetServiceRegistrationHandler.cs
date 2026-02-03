using System.Globalization;
using MediatR;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed class GetServiceRegistrationHandler(
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignmentRepo,
    IStorageService storage,
    IManualPaymentProofRepository proofRepo
) : IRequestHandler<GetServiceRegistrationQuery, GetServiceRegistrationResponse?>
{
    public async Task<GetServiceRegistrationResponse?> Handle(
        GetServiceRegistrationQuery q,
        CancellationToken ct)
    {
        var r = await regRepo.GetByIdAsync(q.RegistrationId, ct);
        if (r is null) return null;
   
        if (r.RetreatId != q.RetreatId) return null;
        
        PreferredSpaceDto? preferredSpaceDto = null;
        if (r.PreferredSpaceId is Guid spaceId)
        {
            var space = await spaceRepo.GetByIdAsync(spaceId, ct);
            if (space is not null)
            {
                preferredSpaceDto = new PreferredSpaceDto(
                    space.Id,
                    space.Name
                );
            }
        }

        ServiceAssignmentDto? assignmentDto = null;
        var assignment = await assignmentRepo.GetByRegistrationIdAsync(r.RetreatId, r.Id, ct);
        if (assignment is not null)
        {
            var assignedSpace = await spaceRepo.GetByIdAsync(assignment.ServiceSpaceId, ct);
            if (assignedSpace is not null)
            {
                assignmentDto = new ServiceAssignmentDto(
                    assignedSpace.Id,
                    assignedSpace.Name,
                    assignment.AssignedAt.UtcDateTime  
                );
            }
        }

        var birthIso = r.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = r.GetAgeOn(today);

        var photoUrl = r.PhotoUrl?.Value;
        if (string.IsNullOrWhiteSpace(photoUrl) && !string.IsNullOrWhiteSpace(r.PhotoStorageKey))
            photoUrl = storage.GetPublicUrl(r.PhotoStorageKey);

        ManualPaymentProofDto? manualProofDto = null;
        var proof = await proofRepo.GetByServiceRegistrationIdAsync(r.Id, ct);
        if (proof is not null)
        {
            manualProofDto = new ManualPaymentProofDto(
                ProofId: proof.Id,
                Amount: proof.Amount.Amount,
                Currency: proof.Amount.Currency,
                Method: proof.Method.ToString(),
                PaymentDate: proof.PaymentDate,
                UploadedAt: proof.ProofUploadedAt,
                Notes: proof.Notes,
                RegisteredBy: proof.RegisteredByUserId,
                RegisteredAt: proof.RegisteredAt
            );
        }

        return new GetServiceRegistrationResponse(
            r.Id,
            (string)r.Name,
            r.Cpf.Value,
            r.Email.Value,
            r.Phone,
            r.City,
            r.Gender.ToString(),
            r.Status.ToString(),
            r.Enabled,
            r.RetreatId,
            birthIso,
            photoUrl,
            r.RegistrationDate,
            preferredSpaceDto,
            assignmentDto,
            age,
            new PersonalDto(
                r.MaritalStatus?.ToString(),
                r.Pregnancy.ToString(),
                r.ShirtSize?.ToString(),
                r.WeightKg,
                r.HeightCm,
                r.Profession,
                r.EducationLevel?.ToString()
            ),
            new AddressContactDto(
                r.StreetAndNumber,
                r.Neighborhood,
                r.State?.ToString(),
                r.PostalCode,
                r.Whatsapp
            ),
            new RahaminExperienceDto(
                r.RahaminVidaCompleted.ToString(),
                r.PreviousUncalledApplications.ToString(),
                r.PostRetreatLifeSummary
            ),
            new SpiritualLifeDto(
                r.ChurchLifeDescription,
                r.PrayerLifeDescription,
                r.FamilyRelationshipDescription,
                r.SelfRelationshipDescription
            ),
            new ConsentDto(
                r.TermsAccepted,
                r.TermsAcceptedAt,
                r.TermsVersion,
                r.MarketingOptIn,
                r.MarketingOptInAt,
                r.ClientIp,
                r.UserAgent
            ),
            new MediaDto(
                r.PhotoStorageKey,
                r.PhotoContentType,
                r.PhotoSizeBytes,
                r.PhotoUploadedAt,
                photoUrl
            ),
            manualProofDto
        );
    }
}
