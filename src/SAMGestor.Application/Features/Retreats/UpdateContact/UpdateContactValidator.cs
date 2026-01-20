using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.UpdateContact;

public class UpdateContactValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Email de contato inválido.")
            .MaximumLength(100).WithMessage("Email de contato não pode exceder 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20).WithMessage("Telefone de contato não pode exceder 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
    }
}