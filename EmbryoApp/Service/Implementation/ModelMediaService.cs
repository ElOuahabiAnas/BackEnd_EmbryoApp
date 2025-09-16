using EmbryoApp.Data;
using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelMedia;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace EmbryoApp.Service.Implementation;

public sealed class ModelMediaService : IModelMediaService
{
    private readonly AuthDbContext _db;
    public ModelMediaService(AuthDbContext db) => _db = db;

    public async Task<ModelMediaResponse?> GetByIdAsync(Guid mediaId, CancellationToken ct)
    {
        var m = await _db.ModelMedia.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MediaId == mediaId, ct);
        return m is null ? null : ToResponse(m);
    }

    public async Task<PagedResult<ModelMediaResponse>> ListByModelAsync(ModelMediaListQuery q, CancellationToken ct)
    {
        var page     = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.ModelMedia.AsNoTracking()
            .Where(m => m.ModelId == q.ModelId);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(m => m.Position ?? int.MaxValue)
            .ThenByDescending(m => m.IsPrimary)
            .ThenBy(m => m.MediaId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            // ⬇️ projection EF -> DTO (expression traduisible en SQL)
            .Select(m => new ModelMediaResponse
            {
                MediaId   = m.MediaId,
                Url       = m.Url,
                MediaType = m.MediaType.ToString(),
                Legende   = m.Legende,
                Position  = m.Position,
                IsPrimary = m.IsPrimary,
                CreatedAt = m.CreatedAt,
                ModelId   = m.ModelId
            })
            .ToListAsync(ct);

        return new PagedResult<ModelMediaResponse>
        {
            Total = total,
            Items = items
        };
    }

    public async Task<Guid> CreateAsync(CreateModelMediaRequest req, CancellationToken ct)
    {
        // 1) vérifier l'existence du parent
        var exists = await _db.Models3D.AsNoTracking()
            .AnyAsync(m => m.ModelId == req.ModelId, ct);
        if (!exists) throw new KeyNotFoundException("model_not_found");

        // 2) unicité du primaire : si IsPrimary = true, on dé-marque les autres
        if (req.IsPrimary)
        {
            await _db.ModelMedia
                .Where(x => x.ModelId == req.ModelId && x.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);
        }

        var entity = new ModelMedia
        {
            MediaId   = Guid.NewGuid(),
            Url       = req.Url.Trim(),
            MediaType = req.MediaType,
            Legende   = string.IsNullOrWhiteSpace(req.Legende) ? null : req.Legende!.Trim(),
            Position  = req.Position,
            IsPrimary = req.IsPrimary,
            // CreatedAt : par défaut DB (NOW()) si configuré dans OnModelCreating
            ModelId   = req.ModelId
        };

        _db.ModelMedia.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.MediaId;
    }

    public async Task<ModelMediaResponse?> UpdateMetaAsync(Guid mediaId, UpdateModelMediaMetaRequest req, CancellationToken ct)
    {
        var m = await _db.ModelMedia.FirstOrDefaultAsync(x => x.MediaId == mediaId, ct);
        if (m is null) return null;

        if (req.Legende != null)
            m.Legende = string.IsNullOrWhiteSpace(req.Legende) ? null : req.Legende.Trim();

        if (req.Position.HasValue)
            m.Position = req.Position;

        if (req.IsPrimary.HasValue)
        {
            if (req.IsPrimary.Value && !m.IsPrimary)
            {
                // si on veut passer en Primary → retirer le flag des autres du même modèle
                await _db.ModelMedia
                    .Where(x => x.ModelId == m.ModelId && x.MediaId != m.MediaId && x.IsPrimary)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);

                m.IsPrimary = true;
            }
            else if (!req.IsPrimary.Value && m.IsPrimary)
            {
                m.IsPrimary = false;
            }
        }

        await _db.SaveChangesAsync(ct);
        return ToResponse(m);
    }


    public async Task<bool> DeleteAsync(Guid mediaId, CancellationToken ct)
    {
        var m = await _db.ModelMedia.FirstOrDefaultAsync(x => x.MediaId == mediaId, ct);
        if (m is null) return false;

        _db.ModelMedia.Remove(m);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static ModelMediaResponse ToResponse(ModelMedia m) => new()
    {
        MediaId   = m.MediaId,
        Url       = m.Url,
        MediaType = m.MediaType.ToString(),
        Legende   = m.Legende,
        Position  = m.Position,
        IsPrimary = m.IsPrimary,
        CreatedAt = m.CreatedAt,
        ModelId   = m.ModelId
    };
}