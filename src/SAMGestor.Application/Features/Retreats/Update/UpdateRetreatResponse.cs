namespace SAMGestor.Application.Features.Retreats.Update;

public sealed record UpdateRetreatResponse(
    Guid Id,
    string Message = "Retiro atualizado com sucesso."
);