using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Service.Registrations.Update;

public sealed record UpdateServiceRegistrationCommand(
    Guid RegistrationId,
    
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
    
    // Foto (Upload separado, mas pode atualizar metadados)
    string? PhotoStorageKey,
    string? PhotoContentType,
    long?   PhotoSize,
    string? PhotoUrl
) : IRequest<UpdateServiceRegistrationResponse>;