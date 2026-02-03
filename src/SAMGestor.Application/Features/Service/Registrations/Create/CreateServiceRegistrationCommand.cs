using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Service.Registrations.Create;

public sealed record CreateServiceRegistrationCommand(
    // Identificação do Retiro
    Guid RetreatId,
    
    // Dados Básicos
    FullName     Name,
    CPF          Cpf,
    EmailAddress Email,
    string       Phone,
    DateOnly     BirthDate,
    Gender       Gender,
    string       City,
    
    // Dados Complementares
    MaritalStatus   MaritalStatus,
    PregnancyStatus Pregnancy,
    ShirtSize       ShirtSize,
    decimal         WeightKg,
    decimal         HeightCm,
    string          Profession,
    EducationLevel  EducationLevel,
    
    // Endereço e Contato
    string StreetAndNumber,
    string Neighborhood,
    UF     State,
    string PostalCode,
    string Whatsapp,
    
    // Experiência Rahamim
    RahaminVidaEdition RahaminVidaCompleted,
    RahaminAttempt     PreviousUncalledApplications,
    string?            PostRetreatLifeSummary,
    
    // Vida Pessoal e Espiritual
    string ChurchLifeDescription,
    string PrayerLifeDescription,
    string FamilyRelationshipDescription,
    string SelfRelationshipDescription,
    
    // Equipe de Serviço
    Guid? PreferredSpaceId,
    
    // Termos e LGPD
    bool    TermsAccepted,
    string  TermsVersion,
    bool?   MarketingOptIn,
    string? ClientIp,
    string? UserAgent,
    
    // Código de Emergência
    string? EmergencyCode = null
) : IRequest<CreateServiceRegistrationResponse>;