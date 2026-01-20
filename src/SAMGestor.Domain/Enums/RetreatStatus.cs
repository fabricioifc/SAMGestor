namespace SAMGestor.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de um retiro
/// </summary>
public enum RetreatStatus
{
    Draft = 0,
    Published = 1,
    RegistrationOpen = 2,
    RegistrationClosed = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}