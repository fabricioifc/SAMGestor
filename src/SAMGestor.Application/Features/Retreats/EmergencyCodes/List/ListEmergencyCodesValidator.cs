using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.List;

public class ListEmergencyCodesValidator : AbstractValidator<ListEmergencyCodesQuery>
{
    public ListEmergencyCodesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");
    }
}