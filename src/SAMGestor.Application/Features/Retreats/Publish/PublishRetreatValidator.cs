using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.Publish;

public class PublishRetreatValidator : AbstractValidator<PublishRetreatCommand>
{
    public PublishRetreatValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}