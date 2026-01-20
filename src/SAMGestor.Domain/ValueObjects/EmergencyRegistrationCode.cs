using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

/// <summary>
/// Value Object que representa um código de emergência para inscrições fora do prazo
/// Permite que gestores autorizem inscrições em casos excepcionais
/// </summary>
public sealed class EmergencyRegistrationCode : ValueObject
{
    /// <summary>
    /// Código alfanumérico único (ex: "EMG-2024-ABC123")
    /// </summary>
    public string Code { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// Data/hora de expiração (null = sem expiração)
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }
    
    public bool IsActive { get; private set; }
    
    public string CreatedByUserId { get; private set; }
    
    public string? Reason { get; private set; }
    
    /// <summary>
    /// Limite de usos do código (null = ilimitado)
    /// </summary>
    public int? MaxUses { get; private set; }
    
    public int UsedCount { get; private set; }

    private EmergencyRegistrationCode() { }

    public EmergencyRegistrationCode(
        string code,
        string createdByUserId,
        int validityDays = 30,
        string? reason = null,
        int? maxUses = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Código é obrigatório.", nameof(code));
        
        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new ArgumentException("ID do criador é obrigatório.", nameof(createdByUserId));
        
        if (validityDays < 0)
            throw new ArgumentException("Validade deve ser maior ou igual a zero.", nameof(validityDays));
        
        if (maxUses.HasValue && maxUses.Value <= 0)
            throw new ArgumentException("Limite de usos deve ser maior que zero.", nameof(maxUses));

        Code = code.Trim().ToUpperInvariant();
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = validityDays > 0 ? DateTime.UtcNow.AddDays(validityDays) : null;
        IsActive = true;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        MaxUses = maxUses;
        UsedCount = 0;
    }
    
    public bool IsValidForUse(DateTime checkDate)
    {
        if (!IsActive) return false;
        if (ExpiresAt.HasValue && checkDate > ExpiresAt.Value) return false;
        if (MaxUses.HasValue && UsedCount >= MaxUses.Value) return false;
        
        return true;
    }
    
    public EmergencyRegistrationCode Deactivate()
    {
        return new EmergencyRegistrationCode
        {
            Code = Code,
            CreatedByUserId = CreatedByUserId,
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            IsActive = false,
            Reason = Reason,
            MaxUses = MaxUses,
            UsedCount = UsedCount
        };
    }
    
    public EmergencyRegistrationCode IncrementUsage()
    {
        return new EmergencyRegistrationCode
        {
            Code = Code,
            CreatedByUserId = CreatedByUserId,
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            IsActive = IsActive,
            Reason = Reason,
            MaxUses = MaxUses,
            UsedCount = UsedCount + 1
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <summary>
    /// Gera um código aleatório no formato "EMG-YYYY-XXXXXX"
    /// </summary>
    public static string GenerateCode()
    {
        var year = DateTime.Now.Year;
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"EMG-{year}-{random}";
    }
}
