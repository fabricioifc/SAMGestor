using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.GetPublicById;

public class GetPublicRetreatByIdValidator : AbstractValidator<GetPublicRetreatByIdQuery>
{
    public GetPublicRetreatByIdValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");
    }
}