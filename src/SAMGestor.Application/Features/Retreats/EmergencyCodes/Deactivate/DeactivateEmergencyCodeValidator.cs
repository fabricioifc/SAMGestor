using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Deactivate;

public class DeactivateEmergencyCodeValidator : AbstractValidator<DeactivateEmergencyCodeCommand>
{
    public DeactivateEmergencyCodeValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório.")
            .MaximumLength(50).WithMessage("Código não pode exceder 50 caracteres.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}