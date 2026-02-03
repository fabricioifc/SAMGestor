namespace SAMGestor.Application.Features.Service.Registrations.Update;

public sealed record UpdateServiceRegistrationResponse(
    Guid    RegistrationId,
    string? PhotoUrl
);