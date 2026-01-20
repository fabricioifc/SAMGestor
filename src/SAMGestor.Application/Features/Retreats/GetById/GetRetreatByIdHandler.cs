using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetById;

public sealed class GetRetreatByIdHandler 
    : IRequestHandler<GetRetreatByIdQuery, GetRetreatByIdResponse>
{
    private readonly IRetreatRepository _repo;

    public GetRetreatByIdHandler(IRetreatRepository repo) => _repo = repo;

    public async Task<GetRetreatByIdResponse> Handle(
        GetRetreatByIdQuery query,
        CancellationToken ct)
    {
        
        var retreat = await _repo.GetByIdWithDetailsAsync(query.Id, ct);
        
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), query.Id);

        return MapToDetailedResponse(retreat);
    }

    private static GetRetreatByIdResponse MapToDetailedResponse(Retreat retreat)
    {
        return new GetRetreatByIdResponse
        {
            
            Id = retreat.Id,
            Name = retreat.Name.Value,
            Edition = retreat.Edition,
            Theme = retreat.Theme,
            ShortDescription = retreat.ShortDescription,
            LongDescription = retreat.LongDescription,
            Location = retreat.Location,

            StartDate = retreat.StartDate,
            EndDate = retreat.EndDate,
            RegistrationStart = retreat.RegistrationStart,
            RegistrationEnd = retreat.RegistrationEnd,

            MaleSlots = retreat.MaleSlots,
            FemaleSlots = retreat.FemaleSlots,
            TotalSlots = retreat.TotalSlots,

            FeeFazerAmount = retreat.FeeFazer.Amount,
            FeeFazerCurrency = retreat.FeeFazer.Currency,
            FeeServirAmount = retreat.FeeServir.Amount,
            FeeServirCurrency = retreat.FeeServir.Currency,

            ContactEmail = retreat.ContactEmail,
            ContactPhone = retreat.ContactPhone,
            
            Status = retreat.Status.ToString(),
            IsPubliclyVisible = retreat.IsPubliclyVisible,
            PublishedAt = retreat.PublishedAt,
            
            ContemplationClosed = retreat.ContemplationClosed,
            
            FamiliesVersion = retreat.FamiliesVersion,
            FamiliesLocked = retreat.FamiliesLocked,
            
            ServiceSpacesVersion = retreat.ServiceSpacesVersion,
            ServiceLocked = retreat.ServiceLocked,
            
            TentsVersion = retreat.TentsVersion,
            TentsLocked = retreat.TentsLocked,
            
            PrivacyPolicy = MapPrivacyPolicy(retreat.PrivacyPolicyData),
            RequiresPrivacyPolicyAcceptance = retreat.RequiresPrivacyPolicyAcceptance,

            Images = retreat.Images
                .Select(img => new RetreatImageDetailDto
                {
                    ImageUrl = img.ImageUrl,
                    StorageId = img.StorageId,
                    Type = img.Type.ToString(),
                    Order = img.Order,
                    UploadedAt = img.UploadedAt,
                    AltText = img.AltText
                })
                .OrderBy(img => img.Order)
                .ToList(),
            
            EmergencyCodes = retreat.EmergencyCodes
                .Select(code => new EmergencyCodeDetailDto
                {
                    Code = code.Code,
                    CreatedAt = code.CreatedAt,
                    ExpiresAt = code.ExpiresAt,
                    IsActive = code.IsActive,
                    CreatedByUserId = code.CreatedByUserId,
                    Reason = code.Reason,
                    MaxUses = code.MaxUses,
                    UsedCount = code.UsedCount,
                    IsExpired = code.ExpiresAt.HasValue && code.ExpiresAt.Value < DateTime.UtcNow,
                    CanBeUsed = code.IsValidForUse(DateTime.UtcNow)
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToList(),

            ActiveEmergencyCodesCount = retreat.GetActiveEmergencyCodes().Count(),
            
            CreatedAt = retreat.CreatedAt,
            CreatedByUserId = retreat.CreatedByUserId,
            LastModifiedAt = retreat.LastModifiedAt,
            LastModifiedByUserId = retreat.LastModifiedByUserId
        };
    }

    private static PrivacyPolicyDetailDto? MapPrivacyPolicy(Domain.ValueObjects.PrivacyPolicy? policy)
    {
        if (policy is null) return null;

        return new PrivacyPolicyDetailDto
        {
            Title = policy.Title,
            Body = policy.Body,
            Version = policy.Version,
            PublishedAt = policy.PublishedAt
        };
    }
}
