namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed record GetServiceRegistrationResponse(
    Guid     Id,
    string   Name,
    string   Cpf,
    string   Email,
    string   Phone,
    string   City,
    string   Gender,
    string   Status,
    bool     Enabled,
    Guid     RetreatId,
    string   BirthDate,
    string?  PhotoUrl,
    DateTime RegistrationDate,
    PreferredSpaceDto? PreferredSpace,
    ServiceAssignmentDto? ServiceAssignment,
    int      Age,
    
    PersonalDto          Personal,
    AddressContactDto    AddressContact,
    RahaminExperienceDto RahaminExperience,
    SpiritualLifeDto     SpiritualLife,
    ConsentDto           Consents,
    MediaDto             Media,
    ManualPaymentProofDto? ManualPaymentProof
);

public sealed record PreferredSpaceDto(
    Guid   Id,
    string Name
);

public sealed record ServiceAssignmentDto(
    Guid     ServiceSpaceId,
    string   ServiceSpaceName,
    DateTime AssignedAt
);

public sealed record PersonalDto(
    string?  MaritalStatus,
    string   Pregnancy,
    string?  ShirtSize,
    decimal? WeightKg,
    decimal? HeightCm,
    string?  Profession,
    string?  EducationLevel
);

public sealed record AddressContactDto(
    string? StreetAndNumber,
    string? Neighborhood,
    string? State,
    string? PostalCode,
    string? Whatsapp
);

public sealed record RahaminExperienceDto(
    string  RahaminVidaCompleted,
    string  PreviousUncalledApplications,
    string? PostRetreatLifeSummary
);

public sealed record SpiritualLifeDto(
    string? ChurchLifeDescription,
    string? PrayerLifeDescription,
    string? FamilyRelationshipDescription,
    string? SelfRelationshipDescription
);

public sealed record ConsentDto(
    bool      TermsAccepted,
    DateTime? TermsAcceptedAt,
    string?   TermsVersion,
    bool      MarketingOptIn,
    DateTime? MarketingOptInAt,
    string?   ClientIp,
    string?   UserAgent
);

public sealed record MediaDto(
    string?   PhotoStorageKey,
    string?   PhotoContentType,
    int?      PhotoSizeBytes,
    DateTime? PhotoUploadedAt,
    string?   PhotoUrl
);

public sealed record ManualPaymentProofDto(
    Guid     ProofId,
    decimal  Amount,
    string   Currency,
    string   Method,
    DateTime PaymentDate,
    DateTime UploadedAt,
    string?  Notes,
    Guid     RegisteredBy,
    DateTime RegisteredAt
);
