namespace EmbryoApp.Service.Implementation;

using EmbryoApp.DTOs;
using EmbryoApp.DTOs.EventLogDtos;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using EmbryoApp.Data;
using Microsoft.EntityFrameworkCore;

public sealed class EventLogService : IEventLogService
{
    private readonly AuthDbContext _db;
    public EventLogService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<EventLogResponse>> ListAsync(EventLogListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var query = _db.Set<EventLog>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.UserId))
            query = query.Where(e => e.UserId == q.UserId);

        if (!string.IsNullOrWhiteSpace(q.EventType))
            query = query.Where(e => e.EventType == q.EventType);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new EventLogResponse
            {
                EventLogId = e.EventLogId,
                EventType = e.EventType,
                Payload = e.Payload,
                CreatedAt = e.CreatedAt,
                UserId = e.UserId
            })
            .ToListAsync(ct);

        return new PagedResult<EventLogResponse> { Total = total, Items = items };
    }

    public async Task<EventLogResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<EventLog>().AsNoTracking()
            .Where(e => e.EventLogId == id)
            .Select(e => new EventLogResponse
            {
                EventLogId = e.EventLogId,
                EventType = e.EventType,
                Payload = e.Payload,
                CreatedAt = e.CreatedAt,
                UserId = e.UserId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(string? userId, CreateEventLogRequest req, CancellationToken ct)
    {
        var entity = new EventLog
        {
            EventLogId = Guid.NewGuid(),
            EventType = req.EventType.Trim(),
            Payload = req.Payload,
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = userId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.EventLogId;
    }
}
