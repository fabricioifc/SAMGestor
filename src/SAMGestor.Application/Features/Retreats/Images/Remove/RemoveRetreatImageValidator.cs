using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.Images.Remove;

public sealed class RemoveRetreatImageValidator : AbstractValidator<RemoveRetreatImageCommand>
{
    public RemoveRetreatImageValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.StorageId)
            .NotEmpty().WithMessage("Identificador de armazenamento é obrigatório.")
            .MaximumLength(500).WithMessage("Identificador de armazenamento não pode exceder 500 caracteres.");

        RuleFor(x => x.RemovedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}