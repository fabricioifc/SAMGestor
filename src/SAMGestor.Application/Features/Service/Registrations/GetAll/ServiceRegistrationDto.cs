namespace SAMGestor.Application.Features.Service.Registrations.GetAll;

public record ServiceRegistrationDto(
    Guid     Id,
    string   Name,
    string   Cpf,
    string   Email,
    string   Phone,
    string   Status,
    string   Gender,
    int      Age,
    string   City,
    string?  State,
    DateTime RegistrationDate,
    string?  PhotoUrl,
    Guid?    PreferredSpaceId,
    string?  PreferredSpaceName,
    Guid?    AssignedSpaceId,
    string?  AssignedSpaceName,
    bool     Enabled
);