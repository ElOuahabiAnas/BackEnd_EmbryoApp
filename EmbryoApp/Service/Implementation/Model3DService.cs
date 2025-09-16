// Features/Models3D/Model3DService.cs
using EmbryoApp.Data;
using EmbryoApp.DTOs;
using EmbryoApp.DTOs.Model3D;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmbryoApp.Service.Implementation;

public sealed class Model3DService : IModel3DService
{
    private readonly AuthDbContext _db;
    private readonly ILogger<Model3DService> _log;
    public Model3DService(AuthDbContext db) => _db = db;


    public Model3DService(AuthDbContext db, ILogger<Model3DService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<Guid> CreateAsync(string authorUserId, CreateModel3DRequest req, CancellationToken ct)
    {
        var entity = new Model3D
        {
            ModelId      = Guid.NewGuid(),
            Title        = req.Title.Trim(),
            Discipline   = string.IsNullOrWhiteSpace(req.Discipline) ? null : req.Discipline!.Trim(),
            EmbryoDay    = req.EmbryoDay,
            Description  = req.Description,
            Status       = req.Status,
            PublishedAt  = req.Status == ModelStatus.Active ? DateTimeOffset.UtcNow : null,
            AuthorUserId = authorUserId
        };

        _db.Models3D.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.ModelId;
    }
    public async Task<Model3DResponse?> GetByIdAsync(Guid modelId, CancellationToken ct)
    {
        var entity = await _db.Models3D.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelId == modelId, ct);
        return entity is null ? null : ToResponse(entity);
    }

    public async Task<PagedResult<Model3DResponse>> ListAsync(Model3DListQuery q, CancellationToken ct)
    {
        var query = _db.Models3D.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(m => m.Title.Contains(s) || (m.Discipline != null && m.Discipline.Contains(s)));
        }
        if (q.Status.HasValue)
            query = query.Where(m => m.Status == q.Status.Value);
        if (!string.IsNullOrWhiteSpace(q.AuthorUserId))
            query = query.Where(m => m.AuthorUserId == q.AuthorUserId);

        var total = await query.CountAsync(ct);
        var skip  = Math.Max(0, (q.Page - 1) * Math.Max(1, q.PageSize));
        var items = await query
            .OrderByDescending(m => m.PublishedAt)
            .ThenByDescending(m => m.ModelId)
            .Skip(skip)
            .Take(Math.Max(1, q.PageSize))
            .Select(m => ToResponse(m))
            .ToListAsync(ct);

        return new PagedResult<Model3DResponse> { Total = total, Items = items };
    }

    public async Task<Model3DResponse?> UpdateAsync(Guid modelId, UpdateModel3DRequest req, CancellationToken ct)
    {
        var entity = await _db.Models3D.FirstOrDefaultAsync(m => m.ModelId == modelId, ct);
        if (entity is null) return null;

        entity.Title       = req.Title.Trim();
        entity.Discipline  = string.IsNullOrWhiteSpace(req.Discipline) ? null : req.Discipline!.Trim();
        entity.EmbryoDay   = req.EmbryoDay;
        entity.Description = req.Description;

        // gérer transition de status + PublishedAt
        if (entity.Status != req.Status)
        {
            entity.Status = req.Status;
            entity.PublishedAt = req.Status == ModelStatus.Active ? (entity.PublishedAt ?? DateTimeOffset.UtcNow) : entity.PublishedAt;
            if (req.Status == ModelStatus.Draft) entity.PublishedAt = null;
        }

        await _db.SaveChangesAsync(ct);
        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(Guid modelId, CancellationToken ct)
    {
        var entity = await _db.Models3D.FirstOrDefaultAsync(m => m.ModelId == modelId, ct);
        if (entity is null) return false;

        _db.Models3D.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Model3D deleted {ModelId}", modelId);
        return true;
    }

    private static Model3DResponse ToResponse(Model3D m) => new Model3DResponse
    {
        ModelId     = m.ModelId,
        Title       = m.Title,
        Discipline  = m.Discipline,
        EmbryoDay   = m.EmbryoDay,
        Description = m.Description,
        Status      = m.Status.ToString(),
        PublishedAt = m.PublishedAt,
        AuthorUserId = m.AuthorUserId
    };
}
