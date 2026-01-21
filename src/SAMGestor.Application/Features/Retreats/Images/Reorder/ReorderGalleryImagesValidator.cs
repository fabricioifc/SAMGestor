using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.Images.Reorder;

public sealed class ReorderGalleryImagesValidator : AbstractValidator<ReorderGalleryImagesCommand>
{
    public ReorderGalleryImagesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.ImageOrders)
            .NotNull().WithMessage("Lista de ordenação é obrigatória.")
            .NotEmpty().WithMessage("Lista de ordenação não pode ser vazia.");

        RuleForEach(x => x.ImageOrders).ChildRules(order =>
        {
            order.RuleFor(o => o.StorageId)
                .NotEmpty().WithMessage("StorageId é obrigatório.");

            order.RuleFor(o => o.NewOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Ordem deve ser maior ou igual a zero.");
        });

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");
    }
}