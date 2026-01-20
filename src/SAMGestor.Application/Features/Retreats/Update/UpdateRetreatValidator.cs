using FluentValidation;
using SAMGestor.Application.Common.Retreat;

namespace SAMGestor.Application.Features.Retreats.Update;

public class UpdateRetreatValidator : BaseRetreatValidator<UpdateRetreatCommand>
{
    public UpdateRetreatValidator() : base()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");
        
        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário modificador é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}