using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.ManageStatus;

public class ManageStatusValidator : AbstractValidator<ManageStatusCommand>
{
    public ManageStatusValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Ação de status inválida.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
        
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Motivo do cancelamento não pode exceder 500 caracteres.")
            .When(x => x.Action == StatusAction.Cancel && !string.IsNullOrWhiteSpace(x.Reason));
    }
}