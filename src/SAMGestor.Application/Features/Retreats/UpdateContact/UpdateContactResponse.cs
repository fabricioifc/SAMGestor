namespace SAMGestor.Application.Features.Retreats.UpdateContact;

public sealed record UpdateContactResponse(
    Guid RetreatId,
    string? ContactEmail,
    string? ContactPhone,
    string Message
);