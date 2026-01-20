using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Retreat : Entity<Guid>
{
    #region Informações Básicas

    public FullName Name { get; private set; }
    public string Edition { get; private set; }
    public string Theme { get; private set; }
    
    /// <summary>
    /// Descrição curta para listagens (máx 200 caracteres)
    /// </summary>
    public string? ShortDescription { get; private set; }
    
 
    public string? LongDescription { get; private set; }
    
    public string? Location { get; private set; }

    #endregion

    #region Datas

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DateOnly RegistrationStart { get; private set; }
    public DateOnly RegistrationEnd { get; private set; }

    #endregion

    #region Vagas

    public int MaleSlots { get; private set; }
    public int FemaleSlots { get; private set; }
    public int TotalSlots => MaleSlots + FemaleSlots;

    #endregion

    #region Taxas

    public Money FeeFazer { get; private set; }
    public Money FeeServir { get; private set; }

    #endregion

    #region Contato

    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }

    #endregion

    #region Status e Controle
    
    public RetreatStatus Status { get; private set; }
    
    /// <summary>
    /// Se o retiro está visível publicamente (frontend)
    /// </summary>
    public bool IsPubliclyVisible { get; private set; }
    
    /// <summary>
    /// Data em que foi publicado/tornado visível
    /// </summary>
    public DateTime? PublishedAt { get; private set; }

    #endregion

    #region Controle de Contemplação

    public bool ContemplationClosed { get; private set; }

    #endregion

    #region Controle de Famílias

    public int FamiliesVersion { get; private set; } = 0;
    public bool FamiliesLocked { get; private set; }

    #endregion

    #region Controle de Serviços

    public int ServiceSpacesVersion { get; private set; } = 0;
    public bool ServiceLocked { get; private set; } = false;

    #endregion

    #region Controle de Barracas/Tendas

    public int TentsVersion { get; private set; } = 0;
    public bool TentsLocked { get; private set; } = false;

    #endregion

    #region Política de Privacidade

    private PrivacyPolicy? _privacyPolicy;
    
    /// <summary>
    /// Política de privacidade do retiro (baseada na LGPD)
    /// </summary>
    public PrivacyPolicy? PrivacyPolicyData 
    { 
        get => _privacyPolicy;
        private set => _privacyPolicy = value;
    }
    
    public bool RequiresPrivacyPolicyAcceptance { get; private set; } = true;
    public string? PrivacyPolicyTitle => _privacyPolicy?.Title;
    public string? PrivacyPolicyBody => _privacyPolicy?.Body;
    public string? PrivacyPolicyVersion => _privacyPolicy?.Version;
    public DateTime? PrivacyPolicyPublishedAt => _privacyPolicy?.PublishedAt;

    #endregion

    #region Imagens

    private readonly List<RetreatImage> _images = new();
    
    /// <summary>
    /// Coleção de imagens associadas ao retiro
    /// </summary>
    public IReadOnlyCollection<RetreatImage> Images => _images.AsReadOnly();

    #endregion

    #region Códigos de Emergência

    private readonly List<EmergencyRegistrationCode> _emergencyCodes = new();
    
    /// <summary>
    /// Códigos de emergência para inscrições fora do prazo
    /// </summary>
    public IReadOnlyCollection<EmergencyRegistrationCode> EmergencyCodes => _emergencyCodes.AsReadOnly();

    #endregion

    #region Auditoria

    public DateTime CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? LastModifiedByUserId { get; private set; }

    #endregion

    #region Construtores

    private Retreat() { }

    public Retreat(
        FullName name,
        string edition,
        string theme,
        DateOnly startDate,
        DateOnly endDate,
        int maleSlots,
        int femaleSlots,
        DateOnly registrationStart,
        DateOnly registrationEnd,
        Money feeFazer,
        Money feeServir,
        string createdByUserId,
        string? shortDescription = null,
        string? longDescription = null,
        string? location = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        ValidateDates(startDate, endDate, registrationStart, registrationEnd);
        ValidateSlots(maleSlots, femaleSlots);
        
        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new ArgumentException("ID do criador é obrigatório.", nameof(createdByUserId));

        Id = Guid.NewGuid();
        Name = name;
        Edition = edition.Trim();
        Theme = theme.Trim();
        StartDate = startDate;
        EndDate = endDate;
        MaleSlots = maleSlots;
        FemaleSlots = femaleSlots;
        RegistrationStart = registrationStart;
        RegistrationEnd = registrationEnd;
        FeeFazer = feeFazer;
        FeeServir = feeServir;

        ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        LongDescription = string.IsNullOrWhiteSpace(longDescription) ? null : longDescription.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();

        Status = RetreatStatus.Draft;
        IsPubliclyVisible = false;
        ContemplationClosed = false;
        FamiliesVersion = 0;
        FamiliesLocked = false;
        
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = DateTime.UtcNow;
        
        _privacyPolicy = PrivacyPolicy.CreateDefault();
    }

    #endregion

    #region Métodos de Atualização

    public void UpdateDetails(
        FullName name,
        string edition,
        string theme,
        DateOnly startDate,
        DateOnly endDate,
        int maleSlots,
        int femaleSlots,
        DateOnly registrationStart,
        DateOnly registrationEnd,
        Money feeFazer,
        Money feeServir,
        string modifiedByUserId,
        string? shortDescription = null,
        string? longDescription = null,
        string? location = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        ValidateDates(startDate, endDate, registrationStart, registrationEnd);
        ValidateSlots(maleSlots, femaleSlots);
        
        if (string.IsNullOrWhiteSpace(modifiedByUserId))
            throw new ArgumentException("ID do modificador é obrigatório.", nameof(modifiedByUserId));

        Name = name;
        Edition = edition.Trim();
        Theme = theme.Trim();
        StartDate = startDate;
        EndDate = endDate;
        MaleSlots = maleSlots;
        FemaleSlots = femaleSlots;
        RegistrationStart = registrationStart;
        RegistrationEnd = registrationEnd;
        FeeFazer = feeFazer;
        FeeServir = feeServir;

        ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        LongDescription = string.IsNullOrWhiteSpace(longDescription) ? null : longDescription.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();

        UpdateAudit(modifiedByUserId);
    }

    public void UpdateContactInfo(string? email, string? phone, string modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(modifiedByUserId))
            throw new ArgumentException("ID do modificador é obrigatório.", nameof(modifiedByUserId));

        ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        ContactPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        
        UpdateAudit(modifiedByUserId);
    }

    #endregion

    #region Gestão de Status e Publicação

    public void Publish(string modifiedByUserId)
    {
        if (!CanBePublished())
            throw new InvalidOperationException("Retiro não pode ser publicado. Verifique se todos os campos obrigatórios estão preenchidos.");

        Status = RetreatStatus.Published;
        IsPubliclyVisible = true;
        PublishedAt = DateTime.UtcNow;
        
        UpdateAudit(modifiedByUserId);
    }

    public void Unpublish(string modifiedByUserId)
    {
        IsPubliclyVisible = false;
        Status = RetreatStatus.Draft;
        
        UpdateAudit(modifiedByUserId);
    }

    public void OpenRegistration(string modifiedByUserId)
    {
        if (Status != RetreatStatus.Published)
            throw new InvalidOperationException("Apenas retiros publicados podem ter inscrições abertas.");

        Status = RetreatStatus.RegistrationOpen;
        UpdateAudit(modifiedByUserId);
    }

    public void CloseRegistration(string modifiedByUserId)
    {
        if (Status != RetreatStatus.RegistrationOpen)
            throw new InvalidOperationException("Apenas retiros com inscrições abertas podem ser fechados.");

        Status = RetreatStatus.RegistrationClosed;
        UpdateAudit(modifiedByUserId);
    }

    public void Start(string modifiedByUserId)
    {
        Status = RetreatStatus.InProgress;
        UpdateAudit(modifiedByUserId);
    }

    public void Complete(string modifiedByUserId)
    {
        Status = RetreatStatus.Completed;
        UpdateAudit(modifiedByUserId);
    }

    public void Cancel(string modifiedByUserId)
    {
        Status = RetreatStatus.Cancelled;
        IsPubliclyVisible = false;
        UpdateAudit(modifiedByUserId);
    }

    #endregion

    #region Gestão de Imagens

    public void AddImage(RetreatImage image, string modifiedByUserId)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
        
        if (image.Type == ImageType.Banner && _images.Any(i => i.Type == ImageType.Banner))
            throw new InvalidOperationException("Já existe uma imagem de banner. Remova-a antes de adicionar outra.");
        
        if (image.Type == ImageType.Thumbnail && _images.Any(i => i.Type == ImageType.Thumbnail))
            throw new InvalidOperationException("Já existe uma imagem de thumbnail. Remova-a antes de adicionar outra.");

        _images.Add(image);
        UpdateAudit(modifiedByUserId);
    }

    public void RemoveImage(string storageId, string modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(storageId))
            throw new ArgumentException("ID de armazenamento é obrigatório.", nameof(storageId));

        var image = _images.FirstOrDefault(i => i.StorageId == storageId);
        if (image == null)
            throw new InvalidOperationException($"Imagem com StorageId '{storageId}' não encontrada.");

        _images.Remove(image);
        UpdateAudit(modifiedByUserId);
    }

    public void ReorderImages(List<(string StorageId, int NewOrder)> reorderList, string modifiedByUserId)
    {
        if (reorderList == null || !reorderList.Any())
            throw new ArgumentException("Lista de reordenação não pode ser vazia.", nameof(reorderList));

        foreach (var (storageId, newOrder) in reorderList)
        {
            var image = _images.FirstOrDefault(i => i.StorageId == storageId);
            if (image == null) continue;

            _images.Remove(image);
            _images.Add(image.WithOrder(newOrder));
        }

        UpdateAudit(modifiedByUserId);
    }

    public RetreatImage? GetBanner() => _images.FirstOrDefault(i => i.Type == ImageType.Banner);
    public RetreatImage? GetThumbnail() => _images.FirstOrDefault(i => i.Type == ImageType.Thumbnail);
    public IEnumerable<RetreatImage> GetGalleryImages() => _images.Where(i => i.Type == ImageType.Gallery).OrderBy(i => i.Order);

    #endregion

    #region Gestão de Códigos de Emergência

    public EmergencyRegistrationCode GenerateEmergencyCode(
        string createdByUserId,
        int validityDays = 30,
        string? reason = null,
        int? maxUses = null)
    {
        var code = EmergencyRegistrationCode.GenerateCode();
        var emergencyCode = new EmergencyRegistrationCode(code, createdByUserId, validityDays, reason, maxUses);
        
        _emergencyCodes.Add(emergencyCode);
        UpdateAudit(createdByUserId);
        
        return emergencyCode;
    }

    public void DeactivateEmergencyCode(string code, string modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código é obrigatório.", nameof(code));

        var existingCode = _emergencyCodes.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (existingCode == null)
            throw new InvalidOperationException($"Código '{code}' não encontrado.");

        _emergencyCodes.Remove(existingCode);
        _emergencyCodes.Add(existingCode.Deactivate());
        
        UpdateAudit(modifiedByUserId);
    }

    public bool ValidateEmergencyCode(string code, DateTime checkDate)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var emergencyCode = _emergencyCodes.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return emergencyCode?.IsValidForUse(checkDate) ?? false;
    }

    public void IncrementEmergencyCodeUsage(string code, string modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código é obrigatório.", nameof(code));

        var existingCode = _emergencyCodes.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (existingCode == null)
            throw new InvalidOperationException($"Código '{code}' não encontrado.");

        _emergencyCodes.Remove(existingCode);
        _emergencyCodes.Add(existingCode.IncrementUsage());
        
        UpdateAudit(modifiedByUserId);
    }

    public IEnumerable<EmergencyRegistrationCode> GetActiveEmergencyCodes()
    {
        return _emergencyCodes.Where(c => c.IsActive && c.IsValidForUse(DateTime.UtcNow));
    }

    #endregion

    #region Política de Privacidade

    public void SetPrivacyPolicy(string title, string body, string version, string modifiedByUserId)
    {
        _privacyPolicy = new PrivacyPolicy(title, body, version);
        UpdateAudit(modifiedByUserId);
    }

    public void SetPrivacyPolicy(PrivacyPolicy policy, string modifiedByUserId)
    {
        _privacyPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        UpdateAudit(modifiedByUserId);
    }

    public void UseDefaultPrivacyPolicy(string modifiedByUserId)
    {
        _privacyPolicy = PrivacyPolicy.CreateDefault();
        UpdateAudit(modifiedByUserId);
    }

    #endregion

    #region Gestão de Contemplação

    public void CloseContemplation() => ContemplationClosed = true;

    #endregion

    #region Gestão de Famílias

    public void BumpFamiliesVersion() => FamiliesVersion++;
    
    public void LockFamilies()
    {
        FamiliesLocked = true;
        BumpFamiliesVersion();
    }

    public void UnlockFamilies()
    {
        FamiliesLocked = false;
        BumpFamiliesVersion();
    }

    #endregion

    #region Gestão de Serviços

    public void BumpServiceSpacesVersion() => ServiceSpacesVersion++;
    
    public void LockService() => ServiceLocked = true;
    
    public void UnlockService() => ServiceLocked = false;

    #endregion

    #region Gestão de Barracas/Tendas

    public void BumpTentsVersion() => TentsVersion++;
    
    public void LockTents()
    {
        TentsLocked = true;
        BumpTentsVersion();
    }

    public void UnlockTents()
    {
        TentsLocked = false;
        BumpTentsVersion();
    }

    #endregion

    #region Regras de Negócio e Validações
    
    public bool CanAcceptRegistrations(DateOnly today, string? emergencyCode = null)
    {
        
        if (Status != RetreatStatus.RegistrationOpen && Status != RetreatStatus.Published)
            return false;
        
        var withinPeriod = RegistrationWindowOpen(today);
        if (withinPeriod)
            return true;
        
        if (!string.IsNullOrWhiteSpace(emergencyCode))
        {
            return ValidateEmergencyCode(emergencyCode, DateTime.UtcNow);
        }

        return false;
    }
    
    public bool RegistrationWindowOpen(DateOnly today) =>
        today >= RegistrationStart && today <= RegistrationEnd;
    
    public bool CanBePublished()
    {
        return !string.IsNullOrWhiteSpace(Theme) &&
               !string.IsNullOrWhiteSpace(Edition) &&
               StartDate > DateOnly.FromDateTime(DateTime.Today) &&
               MaleSlots > 0 &&
               FemaleSlots > 0 &&
               _privacyPolicy != null;
    }

    
    public bool IsActive() => Status != RetreatStatus.Cancelled && Status != RetreatStatus.Completed;

    private static void ValidateDates(DateOnly startDate, DateOnly endDate, DateOnly registrationStart, DateOnly registrationEnd)
    {
        if (endDate < startDate)
            throw new ArgumentException("Data de término deve ser posterior à data de início.", nameof(endDate));

        if (registrationEnd < registrationStart)
            throw new ArgumentException("Data de fim das inscrições deve ser posterior à data de início das inscrições.", nameof(registrationEnd));

        if (startDate <= registrationEnd)
            throw new ArgumentException("Retiro deve começar após o término das inscrições.", nameof(startDate));
    }

    private static void ValidateSlots(int maleSlots, int femaleSlots)
    {
        if (maleSlots < 0)
            throw new ArgumentException("Vagas masculinas não podem ser negativas.", nameof(maleSlots));

        if (femaleSlots < 0)
            throw new ArgumentException("Vagas femininas não podem ser negativas.", nameof(femaleSlots));

        if (maleSlots == 0 && femaleSlots == 0)
            throw new ArgumentException("Deve haver pelo menos uma vaga disponível.");
    }

    private void UpdateAudit(string modifiedByUserId)
    {
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedByUserId = modifiedByUserId.Trim();
    }

    #endregion
}
