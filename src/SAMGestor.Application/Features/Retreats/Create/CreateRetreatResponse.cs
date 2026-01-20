namespace SAMGestor.Application.Features.Retreats.Create;

public sealed record CreateRetreatResponse(
    Guid RetreatId,
    string Message = "Retiro criado com sucesso. Status: Rascunho."
);