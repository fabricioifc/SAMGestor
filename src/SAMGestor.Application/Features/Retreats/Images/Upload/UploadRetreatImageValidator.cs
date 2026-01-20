using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.Images.Upload;

public sealed class UploadRetreatImageValidator : AbstractValidator<UploadRetreatImageCommand>
{
    public UploadRetreatImageValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("Identificador do retiro é obrigatório.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de imagem inválido.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("Arquivo é obrigatório.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Arquivo vazio.")
            .LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Arquivo muito grande (máx 5MB).");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Nome do arquivo é obrigatório.")
            .Must(name => new[] { ".jpg", ".jpeg", ".png" }.Any(ext =>
                name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Formato inválido. Use JPG ou PNG.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type é obrigatório.")
            .Must(ct => new[] { "image/jpeg", "image/png" }.Contains(ct.ToLowerInvariant()))
            .WithMessage("Content type inválido. Use image/jpeg ou image/png.");

        RuleFor(x => x.UploadedByUserId)
            .NotEmpty().WithMessage("Identificador do usuário é obrigatório.")
            .MaximumLength(100).WithMessage("Identificador do usuário não pode exceder 100 caracteres.");

        RuleFor(x => x.AltText)
            .MaximumLength(200).WithMessage("Texto alternativo não pode exceder 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.AltText));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Ordem deve ser maior ou igual a zero.");
    }
}