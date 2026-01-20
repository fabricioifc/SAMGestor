using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Retreats.GetPublicById;

public sealed class GetPublicRetreatByIdHandler 
    : IRequestHandler<GetPublicRetreatByIdQuery, PublicRetreatResponse>
{
    private readonly IRetreatRepository _repo;

    public GetPublicRetreatByIdHandler(IRetreatRepository repo)
    {
        _repo = repo;
    }

    public async Task<PublicRetreatResponse> Handle(
        GetPublicRetreatByIdQuery query,
        CancellationToken ct)
    {
       
        var retreat = await _repo.GetByIdWithDetailsAsync(query.RetreatId, ct);
        
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), query.RetreatId);
        
        if (!retreat.IsPubliclyVisible)
        {
            throw new BusinessRuleException(
                "Este retiro não está disponível para visualização pública.");
        }
        
        return MapToPublicResponse(retreat);
    }

    private static PublicRetreatResponse MapToPublicResponse(Retreat retreat)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        
        return new PublicRetreatResponse
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
            IsRegistrationOpen = retreat.RegistrationWindowOpen(today),
            CanAcceptRegistrations = retreat.CanAcceptRegistrations(today),
            
            Banner = MapImageDto(retreat.GetBanner()),
            Thumbnail = MapImageDto(retreat.GetThumbnail()),
            GalleryImages = retreat.GetGalleryImages()
                .Select(MapImageDto)
                .Where(img => img != null)
                .Cast<RetreatImageDto>()
                .ToList(),
            
            PrivacyPolicy = MapPrivacyPolicyDto(retreat.PrivacyPolicyData),
            RequiresPrivacyPolicyAcceptance = retreat.RequiresPrivacyPolicyAcceptance
        };
    }

    private static RetreatImageDto? MapImageDto(Domain.ValueObjects.RetreatImage? image)
    {
        if (image is null) return null;

        return new RetreatImageDto
        {
            ImageUrl = image.ImageUrl,
            Type = image.Type.ToString(),
            Order = image.Order,
            AltText = image.AltText
        };
    }

    private static PrivacyPolicyDto? MapPrivacyPolicyDto(Domain.ValueObjects.PrivacyPolicy? policy)
    {
        if (policy is null) return null;

        return new PrivacyPolicyDto
        {
            Title = policy.Title,
            Body = policy.Body,
            Version = policy.Version,
            PublishedAt = policy.PublishedAt
        };
    }
}
