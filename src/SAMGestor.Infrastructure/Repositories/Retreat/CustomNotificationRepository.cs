using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Retreat;

public sealed class CustomNotificationRepository : ICustomNotificationRepository
{
    private readonly SAMContext _db;
    
    public CustomNotificationRepository(SAMContext db) => _db = db;

    public async Task AddAsync(CustomNotification notification, CancellationToken ct = default)
        => await _db.CustomNotifications.AddAsync(notification, ct);

    public async Task<CustomNotification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.CustomNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task UpdateAsync(CustomNotification notification, CancellationToken ct = default)
    {
        _db.CustomNotifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<List<CustomNotification>> ListByRetreatAsync(
        Guid retreatId,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        return await _db.CustomNotifications
            .AsNoTracking()
            .Where(n => n.RetreatId == retreatId)
            .OrderByDescending(n => n.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> CountByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => await _db.CustomNotifications
            .AsNoTracking()
            .CountAsync(n => n.RetreatId == retreatId, ct);
}