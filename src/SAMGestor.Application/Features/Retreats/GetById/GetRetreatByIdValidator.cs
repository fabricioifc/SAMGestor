using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.GetById;

public class GetRetreatByIdValidator : AbstractValidator<GetRetreatByIdQuery>
{
    public GetRetreatByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");
    }
}