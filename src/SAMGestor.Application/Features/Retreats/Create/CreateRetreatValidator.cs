using FluentValidation;
using SAMGestor.Application.Common.Retreat;

namespace SAMGestor.Application.Features.Retreats.Create;

public class CreateRetreatValidator : BaseRetreatValidator<CreateRetreatCommand>
{
    public CreateRetreatValidator() : base()
    {
        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário criador é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}