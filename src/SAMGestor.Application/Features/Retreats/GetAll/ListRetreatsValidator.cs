using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public class ListRetreatsValidator : AbstractValidator<ListRetreatsQuery>
{
    public ListRetreatsValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0).WithMessage("Skip deve ser maior ou igual a 0.");

        RuleFor(x => x.Take)
            .GreaterThan(0).WithMessage("Take deve ser maior que 0.")
            .LessThanOrEqualTo(100).WithMessage("Take não pode exceder 100.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.")
            .When(x => x.Status.HasValue);
    }
}