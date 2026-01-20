using FluentValidation;

namespace SAMGestor.Application.Common.Retreat;

public abstract class BaseRetreatValidator<T> : AbstractValidator<T>
    where T : IRetreatCommand
{
    protected BaseRetreatValidator()
    {
        
        RuleFor(x => x.Name)
            .NotNull().WithMessage("Nome é obrigatório.");

        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome não pode ser vazio.")
            .MaximumLength(120).WithMessage("Nome não pode exceder 120 caracteres.")
            .When(x => x.Name is not null);
        
        RuleFor(x => x.Edition)
            .NotEmpty().WithMessage("Edição é obrigatória.")
            .MaximumLength(30).WithMessage("Edição não pode exceder 30 caracteres.");
        
        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Tema é obrigatório.")
            .MaximumLength(120).WithMessage("Tema não pode exceder 120 caracteres.");
        
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data de início é obrigatória.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Data de início deve ser futura.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data de término é obrigatória.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("Data de término deve ser posterior à data de início.");
        
        RuleFor(x => x.RegistrationStart)
            .NotEmpty().WithMessage("Data de início das inscrições é obrigatória.");

        RuleFor(x => x.RegistrationEnd)
            .NotEmpty().WithMessage("Data de término das inscrições é obrigatória.")
            .GreaterThan(x => x.RegistrationStart)
            .WithMessage("Data de término das inscrições deve ser posterior à data de início.");
        
        RuleFor(x => x.StartDate)
            .GreaterThan(x => x.RegistrationEnd)
            .WithMessage("O retiro deve começar após o término das inscrições.");
        
        RuleFor(x => x.MaleSlots)
            .GreaterThanOrEqualTo(0).WithMessage("Vagas masculinas devem ser maiores ou iguais a 0.");

        RuleFor(x => x.FemaleSlots)
            .GreaterThanOrEqualTo(0).WithMessage("Vagas femininas devem ser maiores ou iguais a 0.");

        RuleFor(x => x)
            .Must(cmd => cmd.MaleSlots > 0 || cmd.FemaleSlots > 0)
            .WithMessage("Deve haver pelo menos uma vaga disponível (masculina ou feminina).");
        
        RuleFor(x => x.FeeFazer)
            .NotNull().WithMessage("Taxa para FAZER é obrigatória.");

        RuleFor(x => x.FeeFazer.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Valor da taxa FAZER deve ser maior ou igual a 0.")
            .When(x => x.FeeFazer is not null);

        RuleFor(x => x.FeeFazer.Currency)
            .NotEmpty().WithMessage("Moeda da taxa FAZER é obrigatória.")
            .When(x => x.FeeFazer is not null);
        
        RuleFor(x => x.FeeServir)
            .NotNull().WithMessage("Taxa para SERVIR é obrigatória.");

        RuleFor(x => x.FeeServir.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Valor da taxa SERVIR deve ser maior ou igual a 0.")
            .When(x => x.FeeServir is not null);

        RuleFor(x => x.FeeServir.Currency)
            .NotEmpty().WithMessage("Moeda da taxa SERVIR é obrigatória.")
            .When(x => x.FeeServir is not null);
        
        RuleFor(x => x.ShortDescription)
            .MaximumLength(200).WithMessage("Descrição curta não pode exceder 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ShortDescription));
        
        RuleFor(x => x.LongDescription)
            .MaximumLength(5000).WithMessage("Descrição longa não pode exceder 5000 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.LongDescription));
        
        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Local não pode exceder 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Location));
        
        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Email de contato inválido.")
            .MaximumLength(100).WithMessage("Email de contato não pode exceder 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        
        RuleFor(x => x.ContactPhone)
            .MaximumLength(20).WithMessage("Telefone de contato não pode exceder 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
    }
}
