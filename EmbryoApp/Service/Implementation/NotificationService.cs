using EmbryoApp.DTOs;
using EmbryoApp.DTOs.NotificationDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Service.Implementation;
// Features/Notifications/NotificationService.cs
using EmbryoApp.Data;
using EmbryoApp.Models;
using Microsoft.EntityFrameworkCore;


public sealed class NotificationService : INotificationService
{
    private readonly AuthDbContext _db;
    public NotificationService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<NotificationResponse>> ListAsync(NotificationListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.Set<Notification>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.UserId))
            baseQuery = baseQuery.Where(n => n.UserId == q.UserId);

        if (q.UnreadOnly == true)
            baseQuery = baseQuery.Where(n => !n.IsRead);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(n => n.SentAt)
            .ThenByDescending(n => n.NotificationId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Body = n.Body,
                SentAt = n.SentAt,
                IsRead = n.IsRead,
                UserId = n.UserId
            })
            .ToListAsync(ct);

        return new PagedResult<NotificationResponse> { Total = total, Items = items };
    }

    public async Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<Notification>().AsNoTracking()
            .Where(n => n.NotificationId == id)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Body = n.Body,
                SentAt = n.SentAt,
                IsRead = n.IsRead,
                UserId = n.UserId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(CreateNotificationRequest req, CancellationToken ct)
    {
        // vérifier que l'utilisateur existe
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == req.UserId, ct);
        if (!exists) throw new KeyNotFoundException("user_not_found");

        var entity = new Notification
        {
            NotificationId = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Body  = req.Body.Trim(),
            SentAt = DateTimeOffset.UtcNow,
            IsRead = false,
            UserId = req.UserId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.NotificationId;
    }

    public async Task<bool> MarkReadAsync(Guid id, string callerUserId, bool isElevated, CancellationToken ct)
    {
        var n = await _db.Set<Notification>().FirstOrDefaultAsync(x => x.NotificationId == id, ct);
        if (n is null) return false;

        if (!isElevated && n.UserId != callerUserId) return false; // ownership

        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string targetUserId, string callerUserId, bool isElevated, CancellationToken ct)
    {
        if (!isElevated && targetUserId != callerUserId) return 0;

        // EF Core 7+ ExecuteUpdateAsync, sinon fallback foreach
        var affected = await _db.Set<Notification>()
            .Where(n => n.UserId == targetUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);

        return affected;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var n = await _db.Set<Notification>().FirstOrDefaultAsync(x => x.NotificationId == id, ct);
        if (n is null) return false;
        _db.Remove(n);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
