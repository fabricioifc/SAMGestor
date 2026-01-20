using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.EmergencyCodes.Generate;

public class GenerateEmergencyCodeValidator : AbstractValidator<GenerateEmergencyCodeCommand>
{
    public GenerateEmergencyCodeValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário criador é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");

        RuleFor(x => x.ValidityDays)
            .GreaterThanOrEqualTo(0).WithMessage("Dias de validade deve ser maior ou igual a 0.")
            .LessThanOrEqualTo(365).WithMessage("Dias de validade não pode exceder 365 dias.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Motivo não pode exceder 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).WithMessage("Limite de usos deve ser maior que 0.")
            .When(x => x.MaxUses.HasValue);
    }
}