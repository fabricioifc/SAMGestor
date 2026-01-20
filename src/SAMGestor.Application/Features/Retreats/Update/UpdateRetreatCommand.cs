using MediatR;
using SAMGestor.Application.Common.Retreat;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Retreats.Update;

public record UpdateRetreatCommand(
    Guid Id,
    FullName Name,
    string Edition,
    string Theme,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaleSlots,
    int FemaleSlots,
    DateOnly RegistrationStart,
    DateOnly RegistrationEnd,
    Money FeeFazer,
    Money FeeServir,
    string ModifiedByUserId, 
    string? ShortDescription = null,
    string? LongDescription = null,
    string? Location = null,
    string? ContactEmail = null,
    string? ContactPhone = null
) : IRequest<UpdateRetreatResponse>, IRetreatCommand;