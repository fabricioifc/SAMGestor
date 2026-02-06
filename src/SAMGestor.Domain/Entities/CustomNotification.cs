using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

/// <summary>
/// Representa uma notificação customizada enviada por gestores
/// Usado para auditoria e rastreamento de envios
/// </summary>
public class CustomNotification : Entity<Guid>
{
    public Guid? RetreatId { get; private set; }
    public Guid SentByUserId { get; private set; }
    public DateTime SentAt { get; private set; }
    
    public NotificationTargetType TargetType { get; private set; }
    
    /// <summary>
    /// JSON com informações do filtro aplicado
    /// Ex: {"UserIds":["guid1","guid2"]} ou {"Module":"Fazer","Statuses":["Selected"]}
    /// </summary>
    
    public string TargetFilterJson { get; private set; }
    
    public NotificationTemplate Template { get; private set; }
    
    public int TotalRecipients { get; private set; }
    public CustomNotificationStatus Status { get; private set; }
    
    public string? FailureReason { get; private set; }

    private CustomNotification() { }

    public CustomNotification(
        Guid? retreatId,
        Guid sentByUserId,
        NotificationTargetType targetType,
        string targetFilterJson,
        NotificationTemplate template,
        int totalRecipients)
    {
        if (sentByUserId == Guid.Empty)
            throw new ArgumentException("SentByUserId é obrigatório", nameof(sentByUserId));
        
        if (string.IsNullOrWhiteSpace(targetFilterJson))
            throw new ArgumentException("TargetFilterJson é obrigatório", nameof(targetFilterJson));
        
        if (totalRecipients <= 0)
            throw new ArgumentException("TotalRecipients deve ser maior que zero", nameof(totalRecipients));

        Id = Guid.NewGuid();
        RetreatId = retreatId;
        SentByUserId = sentByUserId;
        SentAt = DateTime.UtcNow;
        TargetType = targetType;
        TargetFilterJson = targetFilterJson.Trim();
        Template = template ?? throw new ArgumentNullException(nameof(template));
        TotalRecipients = totalRecipients;
        Status = CustomNotificationStatus.Queued;
    }

    public void MarkAsSending()
    {
        if (Status != CustomNotificationStatus.Queued)
            throw new InvalidOperationException($"Não é possível marcar como Sending. Status atual: {Status}");
        
        Status = CustomNotificationStatus.Sending;
    }

    public void MarkAsSent()
    {
        if (Status != CustomNotificationStatus.Sending)
            throw new InvalidOperationException($"Não é possível marcar como Sent. Status atual: {Status}");
        
        Status = CustomNotificationStatus.Sent;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason é obrigatório ao marcar como Failed", nameof(reason));
        
        Status = CustomNotificationStatus.Failed;
        FailureReason = reason.Trim();
    }
}
