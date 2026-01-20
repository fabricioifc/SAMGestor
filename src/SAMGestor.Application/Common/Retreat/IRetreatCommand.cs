using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Common.Retreat;

public interface IRetreatCommand
{
    FullName Name { get; }
    string Edition { get; }
    string Theme { get; }
    DateOnly StartDate { get; }
    DateOnly EndDate { get; }
    int MaleSlots { get; }
    int FemaleSlots { get; }
    DateOnly RegistrationStart { get; }
    DateOnly RegistrationEnd { get; }
    Money FeeFazer { get; }
    Money FeeServir { get; }
    string? ShortDescription { get; }
    string? LongDescription { get; }
    string? Location { get; }
    string? ContactEmail { get; }
    string? ContactPhone { get; }
}