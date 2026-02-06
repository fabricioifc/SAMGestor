namespace SAMGestor.Domain.Enums;

/// <summary>
/// Define o tipo de destinatários da notificação customizada
/// </summary>
public enum NotificationTargetType
{
    /// <summary>
    /// Usuários específicos escolhidos pelo gestor (por ID)
    /// </summary>
    SpecificUsers = 1,
    
    /// <summary>
    /// Filtro por módulo(s) e status do evento
    /// </summary>
    ModuleFilter = 2,
    
    /// <summary>
    /// Usuários administrativos do sistema (gestores/admins/consultores)
    /// </summary>
    AdminUsers = 3
}