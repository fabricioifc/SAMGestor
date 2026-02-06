using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface ICustomNotificationRepository
{
    Task AddAsync(CustomNotification notification, CancellationToken ct = default);
    Task<CustomNotification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(CustomNotification notification, CancellationToken ct = default);
    
    Task<List<CustomNotification>> ListByRetreatAsync(
        Guid retreatId, 
        int skip = 0, 
        int take = 50,
        CancellationToken ct = default);
    
    Task<int> CountByRetreatAsync(Guid retreatId, CancellationToken ct = default);
}