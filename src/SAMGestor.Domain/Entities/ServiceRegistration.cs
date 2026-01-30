using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class ServiceRegistration : Entity<Guid>
{
    #region Informações Básicas
    
    public FullName Name { get; private set; } = default!;
    public CPF Cpf { get; private set; }
    public EmailAddress Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public DateOnly BirthDate { get; private set; }
    public Gender Gender { get; private set; }
    public string City { get; private set; } = default!;
    
    #endregion

    #region Controle e Status
    
    public Guid RetreatId { get; private set; }
    public Guid? PreferredSpaceId { get; private set; }
    public ServiceRegistrationStatus Status { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime RegistrationDate { get; private set; }
    
    #endregion

    #region Termos e LGPD
    
    public bool TermsAccepted { get; private set; }
    public DateTime? TermsAcceptedAt { get; private set; }
    public string? TermsVersion { get; private set; }
    public bool MarketingOptIn { get; private set; }
    public DateTime? MarketingOptInAt { get; private set; }
    public string? ClientIp { get; private set; }
    public string? UserAgent { get; private set; }
    
    #endregion

    #region Dados Complementares
    
    public MaritalStatus? MaritalStatus { get; private set; }
    public PregnancyStatus Pregnancy { get; private set; } = PregnancyStatus.None;
    public ShirtSize? ShirtSize { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public string? Profession { get; private set; }
    public EducationLevel? EducationLevel { get; private set; }
    
    #endregion

    #region Endereço e Contato
    
    public string? StreetAndNumber { get; private set; }
    public string? Neighborhood { get; private set; }
    public UF? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Whatsapp { get; private set; }
    
    #endregion

    #region Experiência Rahamim
    
    public RahaminVidaEdition RahaminVidaCompleted { get; private set; } = RahaminVidaEdition.None;
    public RahaminAttempt PreviousUncalledApplications { get; private set; } = RahaminAttempt.None;
    public string? PostRetreatLifeSummary { get; private set; }
    
    #endregion

    #region Vida Pessoal e Espiritual
    
    public string? ChurchLifeDescription { get; private set; }
    public string? PrayerLifeDescription { get; private set; }
    public string? FamilyRelationshipDescription { get; private set; }
    public string? SelfRelationshipDescription { get; private set; }
    
    #endregion

    #region Foto
    
    public UrlAddress? PhotoUrl { get; private set; }
    public string? PhotoStorageKey { get; private set; }
    public string? PhotoContentType { get; private set; }
    public int? PhotoSizeBytes { get; private set; }
    public DateTime? PhotoUploadedAt { get; private set; }
    
    #endregion

    #region Construtores

    private ServiceRegistration() { }

    public ServiceRegistration(
        Guid retreatId,
        FullName name,
        CPF cpf,
        EmailAddress email,
        string phone,
        DateOnly birthDate,
        Gender gender,
        string city,
        Guid? preferredSpaceId = null)
    {
        Id = Guid.NewGuid();
        RetreatId = retreatId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Cpf = cpf;
        Email = email;
        Phone = phone.Trim();
        BirthDate = birthDate;
        Gender = gender;
        City = city.Trim();
        PreferredSpaceId = preferredSpaceId;
        
        Status = ServiceRegistrationStatus.Submitted;
        Enabled = true;
        RegistrationDate = DateTime.UtcNow;
        
        // Inicializar valores default
        TermsAccepted = false;
        MarketingOptIn = false;
        Pregnancy = PregnancyStatus.None;
        RahaminVidaCompleted = RahaminVidaEdition.None;
        PreviousUncalledApplications = RahaminAttempt.None;
    }

    #endregion

    #region Métodos de Atualização - Dados Básicos

    public void UpdateBasicInfo(
        FullName name,
        CPF cpf,
        EmailAddress email,
        string phone,
        DateOnly birthDate,
        Gender gender,
        string city)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Cpf = cpf;
        Email = email;
        Phone = phone.Trim();
        BirthDate = birthDate;
        Gender = gender;
        City = city.Trim();
    }

    #endregion

    #region Métodos de Atualização - Dados Complementares

    public void SetMaritalStatus(MaritalStatus? status) => MaritalStatus = status;
    
    public void SetPregnancy(PregnancyStatus status) => Pregnancy = status;
    
    public void SetShirtSize(ShirtSize? size) => ShirtSize = size;
    
    public void SetAnthropometrics(decimal? weightKg, decimal? heightCm)
    {
        WeightKg = weightKg;
        HeightCm = heightCm;
    }
    
    public void SetProfession(string? profession)
        => Profession = string.IsNullOrWhiteSpace(profession) ? null : profession.Trim();
    
    public void SetEducationLevel(EducationLevel? level) => EducationLevel = level;

    #endregion

    #region Métodos de Atualização - Endereço e Contato

    public void SetAddress(
        string? streetAndNumber, 
        string? neighborhood, 
        UF? state, 
        string? postalCode, 
        string? city = null)
    {
        StreetAndNumber = string.IsNullOrWhiteSpace(streetAndNumber) ? null : streetAndNumber.Trim();
        Neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : neighborhood.Trim();
        State = state;
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        if (!string.IsNullOrWhiteSpace(city)) City = city!.Trim();
    }
    
    public void SetWhatsapp(string? value)
        => Whatsapp = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    #endregion

    #region Métodos de Atualização - Experiência Rahamim

    public void SetRahaminVidaCompleted(RahaminVidaEdition edition)
        => RahaminVidaCompleted = edition;
    
    public void SetPreviousUncalledApplications(RahaminAttempt attempts)
        => PreviousUncalledApplications = attempts;
    
    public void SetPostRetreatLifeSummary(string? summary)
        => PostRetreatLifeSummary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();

    #endregion

    #region Métodos de Atualização - Vida Pessoal e Espiritual

    public void SetChurchLifeDescription(string? description)
        => ChurchLifeDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    
    public void SetPrayerLifeDescription(string? description)
        => PrayerLifeDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    
    public void SetFamilyRelationshipDescription(string? description)
        => FamilyRelationshipDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    
    public void SetSelfRelationshipDescription(string? description)
        => SelfRelationshipDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    #endregion

    #region Métodos de Atualização - Foto

    public void SetPhoto(
        string storageKey,
        string? contentType,
        int? sizeBytes,
        DateTime uploadedAt,
        UrlAddress? publicUrl = null)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key é obrigatório.", nameof(storageKey));
            
        PhotoStorageKey = storageKey;
        PhotoContentType = contentType;
        PhotoSizeBytes = sizeBytes;
        PhotoUploadedAt = uploadedAt;
        if (publicUrl is not null) PhotoUrl = publicUrl;
    }

    #endregion

    #region Métodos de Atualização - Termos e LGPD

    public void AcceptTerms(string versionOrHash, DateTime acceptedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(versionOrHash))
            throw new ArgumentException("Versão/identificador da política é obrigatório.", nameof(versionOrHash));
        
        TermsAccepted = true;
        TermsAcceptedAt = acceptedAtUtc;
        TermsVersion = versionOrHash.Trim();
    }
    
    public void SetMarketingOptIn(bool optIn, DateTime utcNow)
    {
        MarketingOptIn = optIn;
        MarketingOptInAt = optIn ? utcNow : null;
    }
    
    public void SetClientContext(string? clientIp, string? userAgent)
    {
        ClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    }

    #endregion

    #region Gestão de Status

    public void MarkNotified()
    {
        if (Status != ServiceRegistrationStatus.Submitted && 
            Status != ServiceRegistrationStatus.Notified)
            throw new InvalidOperationException("Transição inválida para Notified.");
        
        Status = ServiceRegistrationStatus.Notified;
    }
    
    public void Confirm()
    {
        if (Status is ServiceRegistrationStatus.Cancelled or ServiceRegistrationStatus.Declined)
            throw new InvalidOperationException("Não é possível confirmar inscrição cancelada/recusada.");
        
        Status = ServiceRegistrationStatus.Confirmed;
    }
    
    public void Decline()
    {
        if (Status == ServiceRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Inscrição já está cancelada.");
        
        Status = ServiceRegistrationStatus.Declined;
    }
    
    public void Cancel()
    {
        if (Status == ServiceRegistrationStatus.Cancelled)
            return;
        
        Status = ServiceRegistrationStatus.Cancelled;
    }
    
    public void ConfirmManualPayment()
    {
        if (Status == ServiceRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Não é possível confirmar pagamento de inscrição cancelada.");
        
        if (Status == ServiceRegistrationStatus.Declined)
            throw new InvalidOperationException("Não é possível confirmar pagamento de inscrição recusada.");
        
        Status = ServiceRegistrationStatus.Confirmed;
    }

    #endregion

    #region Gestão de Estado

    public void Disable() => Enabled = false;
    
    public void Enable() => Enabled = true;
    
    public void UpdatePreferredSpace(Guid? preferredSpaceId)
        => PreferredSpaceId = preferredSpaceId;

    #endregion

    #region Regras de Negócio e Cálculos

    public int GetAgeOn(DateOnly onDate)
    {
        int age = onDate.Year - BirthDate.Year;
        if (new DateOnly(onDate.Year, BirthDate.Month, BirthDate.Day) > onDate) age--;
        return age;
    }
    
    public bool IsEligibleForService() =>
        Enabled && Status == ServiceRegistrationStatus.Confirmed;

    #endregion
}
