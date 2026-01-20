using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.UpdatePrivacyPolicy;

public class UpdatePrivacyPolicyValidator : AbstractValidator<UpdatePrivacyPolicyCommand>
{
    public UpdatePrivacyPolicyValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título da política é obrigatório.")
            .MaximumLength(200).WithMessage("Título não pode exceder 200 caracteres.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Conteúdo da política é obrigatório.")
            .MaximumLength(50000).WithMessage("Conteúdo não pode exceder 50000 caracteres.");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Versão da política é obrigatória.")
            .MaximumLength(50).WithMessage("Versão não pode exceder 50 caracteres.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}