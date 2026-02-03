using FluentValidation;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Registrations.Update;

public class UpdateServiceRegistrationValidator : AbstractValidator<UpdateServiceRegistrationCommand>
{
    public UpdateServiceRegistrationValidator()
    {
        #region Identificação
        
        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("ID da inscrição é obrigatório");
        
        #endregion

        #region Dados Básicos
        
        RuleFor(x => x.Name)
            .NotNull().WithMessage("Nome é obrigatório");
        
        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Nome não pode ser vazio")
            .MaximumLength(160).WithMessage("Nome não pode ter mais de 160 caracteres");

        RuleFor(x => x.Cpf)
            .NotNull().WithMessage("CPF é obrigatório");
        
        RuleFor(x => x.Email)
            .NotNull().WithMessage("E-mail é obrigatório");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefone é obrigatório")
            .MaximumLength(40).WithMessage("Telefone não pode ter mais de 40 caracteres")
            .Must(IsPhoneDigits).WithMessage("Telefone deve ter 10 ou 11 dígitos");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Data de nascimento é obrigatória")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Data de nascimento deve ser anterior à data atual")
            .Must(BeValidAge).WithMessage("É necessário ter entre 18 e 80 anos");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Gênero inválido");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Cidade é obrigatória")
            .MaximumLength(120).WithMessage("Cidade não pode ter mais de 120 caracteres");
        
        #endregion

        #region Dados Complementares
        
        RuleFor(x => x.MaritalStatus)
            .IsInEnum().WithMessage("Estado civil inválido");
        
        RuleFor(x => x.Pregnancy)
            .IsInEnum().WithMessage("Status de gravidez inválido");
        
        When(x => x.Gender != Gender.Female, () =>
        {
            RuleFor(x => x.Pregnancy)
                .Equal(PregnancyStatus.None)
                .WithMessage("Apenas mulheres podem informar gravidez");
        });
        
        RuleFor(x => x.ShirtSize)
            .IsInEnum().WithMessage("Tamanho de camiseta inválido");
        
        RuleFor(x => x.WeightKg)
            .GreaterThan(30).WithMessage("Peso mínimo: 30kg")
            .LessThanOrEqualTo(300).WithMessage("Peso máximo: 300kg");
        
        RuleFor(x => x.HeightCm)
            .GreaterThan(100).WithMessage("Altura mínima: 100cm")
            .LessThanOrEqualTo(250).WithMessage("Altura máxima: 250cm");
        
        RuleFor(x => x.Profession)
            .NotEmpty().WithMessage("Profissão é obrigatória")
            .MaximumLength(120).WithMessage("Profissão não pode ter mais de 120 caracteres");
        
        RuleFor(x => x.EducationLevel)
            .IsInEnum().WithMessage("Escolaridade inválida");
        
        #endregion

        #region Endereço e Contato
        
        RuleFor(x => x.StreetAndNumber)
            .NotEmpty().WithMessage("Endereço é obrigatório")
            .MaximumLength(160).WithMessage("Endereço não pode ter mais de 160 caracteres");
        
        RuleFor(x => x.Neighborhood)
            .NotEmpty().WithMessage("Bairro é obrigatório")
            .MaximumLength(120).WithMessage("Bairro não pode ter mais de 120 caracteres");
        
        RuleFor(x => x.State)
            .IsInEnum().WithMessage("Estado inválido");
        
        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("CEP é obrigatório")
            .Matches(@"^\d{5}-?\d{3}$").WithMessage("CEP inválido. Use o formato: 99999-999");
        
        RuleFor(x => x.Whatsapp)
            .NotEmpty().WithMessage("WhatsApp é obrigatório")
            .MaximumLength(20).WithMessage("WhatsApp não pode ter mais de 20 caracteres")
            .Must(IsPhoneDigits).WithMessage("WhatsApp deve ter 10 ou 11 dígitos");
        
        #endregion

        #region Experiência Rahamim
        
        RuleFor(x => x.RahaminVidaCompleted)
            .IsInEnum().WithMessage("Rahamim Vida completado inválido");
        
        RuleFor(x => x.PreviousUncalledApplications)
            .IsInEnum().WithMessage("Tentativas anteriores inválidas");
        
        When(x => x.RahaminVidaCompleted != RahaminVidaEdition.None, () =>
        {
            RuleFor(x => x.PostRetreatLifeSummary)
                .NotEmpty().WithMessage("Conte como está sua vida após o Rahamim")
                .MaximumLength(1000).WithMessage("Resumo não pode ter mais de 1000 caracteres");
        });
        
        #endregion

        #region Vida Pessoal e Espiritual
        
        RuleFor(x => x.ChurchLifeDescription)
            .NotEmpty().WithMessage("Conte sobre sua vida na igreja")
            .MinimumLength(50).WithMessage("Escreva pelo menos 50 caracteres")
            .MaximumLength(1000).WithMessage("Descrição não pode ter mais de 1000 caracteres");
        
        RuleFor(x => x.PrayerLifeDescription)
            .NotEmpty().WithMessage("Conte sobre sua vida de oração")
            .MinimumLength(50).WithMessage("Escreva pelo menos 50 caracteres")
            .MaximumLength(1000).WithMessage("Descrição não pode ter mais de 1000 caracteres");
        
        RuleFor(x => x.FamilyRelationshipDescription)
            .NotEmpty().WithMessage("Conte sobre sua relação com a família")
            .MinimumLength(50).WithMessage("Escreva pelo menos 50 caracteres")
            .MaximumLength(1000).WithMessage("Descrição não pode ter mais de 1000 caracteres");
        
        RuleFor(x => x.SelfRelationshipDescription)
            .NotEmpty().WithMessage("Conte sobre sua relação consigo mesmo")
            .MinimumLength(50).WithMessage("Escreva pelo menos 50 caracteres")
            .MaximumLength(1000).WithMessage("Descrição não pode ter mais de 1000 caracteres");
        
        #endregion

        #region Foto (opcional no Update)
        
        When(x => !string.IsNullOrWhiteSpace(x.PhotoStorageKey), () =>
        {
            RuleFor(x => x.PhotoUrl)
                .NotEmpty().WithMessage("URL da foto é obrigatória quando StorageKey é fornecido");
            
            RuleFor(x => x.PhotoContentType)
                .NotEmpty().WithMessage("Content Type da foto é obrigatório");
            
            RuleFor(x => x.PhotoSize)
                .NotNull().WithMessage("Tamanho da foto é obrigatório")
                .GreaterThan(0).WithMessage("Tamanho da foto deve ser maior que 0");
        });
        
        #endregion
    }

    private static bool IsPhoneDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 11;
    }

    private static bool BeValidAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Year;
        if (new DateOnly(today.Year, birthDate.Month, birthDate.Day) > today)
            age--;
        
        return age >= 18 && age <= 80;
    }
}
