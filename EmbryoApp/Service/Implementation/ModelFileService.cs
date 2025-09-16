using EmbryoApp.Data;
using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelFiles;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace EmbryoApp.Service.Implementation;

public sealed class ModelFileService : IModelFileService
{
    private readonly AuthDbContext _db;
    private readonly IWebHostEnvironment _env;

    
    public ModelFileService(AuthDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }
    
    public async Task<ModelFileResponse?> GetByIdAsync(Guid fileId, CancellationToken ct)
    {
        var f = await _db.ModelFiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileId == fileId, ct);
        return f is null ? null : ToResponse(f);
    }

    public async Task<PagedResult<ModelFileResponse>> ListByModelAsync(ModelFileListQuery q, CancellationToken ct)
    {
        var page     = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var query = _db.ModelFiles.AsNoTracking()
            .Where(f => f.ModelId == q.ModelId)
            .OrderBy(f => f.Position ?? int.MaxValue) // ceux sans position à la fin
            .ThenByDescending(f => f.IsPrimary)
            .ThenBy(f => f.FileId);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(f => ToResponse(f))
                               .ToListAsync(ct);

        return new PagedResult<ModelFileResponse> { Total = total, Items = items };
    }

    public async Task<Guid> CreateAsync(CreateModelFileRequest req, CancellationToken ct)
    {
        // 1) vérifier l'existence du parent
        var exists = await _db.Models3D.AsNoTracking()
            .AnyAsync(m => m.ModelId == req.ModelId, ct);
        if (!exists) throw new KeyNotFoundException("model_not_found");

        // 2) optionnel: si IsPrimary = true, dé-marquer les autres fichiers de ce model
        if (req.IsPrimary)
        {
            await _db.ModelFiles
                .Where(f => f.ModelId == req.ModelId && f.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsPrimary, false), ct);
        }

        var entity = new ModelFile
        {
            FileId    = Guid.NewGuid(),
            Path      = req.Path.Trim(),
            FileType  = string.IsNullOrWhiteSpace(req.FileType) ? null : req.FileType!.Trim(),
            FileRole  = string.IsNullOrWhiteSpace(req.FileRole) ? null : req.FileRole!.Trim(),
            IsPrimary = req.IsPrimary,
            Position  = req.Position,
            // CreatedAt: par défaut DB (NOW()), si tu veux le fixer côté code :
            // CreatedAt = DateTimeOffset.UtcNow,
            ModelId   = req.ModelId
        };

        _db.ModelFiles.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.FileId;
    }

    public async Task<ModelFileResponse?> UpdateMetaAsync(Guid fileId, UpdateModelFileMetaRequest req, CancellationToken ct)
    {
        var f = await _db.ModelFiles.FirstOrDefaultAsync(x => x.FileId == fileId, ct);
        if (f is null) return null;

        // FileRole
        if (req.FileRole != null)
            f.FileRole = string.IsNullOrWhiteSpace(req.FileRole) ? null : req.FileRole.Trim();

        // Position
        if (req.Position.HasValue)
            f.Position = req.Position;

        // IsPrimary (unicité par ModelId)
        if (req.IsPrimary.HasValue)
        {
            if (req.IsPrimary.Value && !f.IsPrimary)
            {
                await _db.ModelFiles
                    .Where(x => x.ModelId == f.ModelId && x.FileId != f.FileId && x.IsPrimary)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct);

                f.IsPrimary = true;
            }
            else if (!req.IsPrimary.Value && f.IsPrimary)
            {
                f.IsPrimary = false;
            }
        }

        await _db.SaveChangesAsync(ct);
        return ToResponse(f); // ta méthode existante qui mappe l’entité -> DTO
    }

    public async Task<bool> DeleteAsync(Guid fileId, CancellationToken ct = default)
    {
        var entity = await _db.ModelFiles
            .FirstOrDefaultAsync(f => f.FileId == fileId, ct);

        if (entity is null) return false;

        // 1) Calculer le chemin absolu dans wwwroot
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        // `Path` en DB est stocké relatif (ex: "/uploads/models/{modelId}/{fileId}.glb"
        var relative = (entity.Path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative));
        var rootFull = Path.GetFullPath(webRoot);

        // Garde-fou: empêcher toute sortie de wwwroot
        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored file path.");

        // 2) Supprimer le fichier s'il existe
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception)
        {
            // TODO: logger l’exception si vous avez un logger
            // on continue quand même pour supprimer la ligne DB
        }

        // 3) Supprimer la ligne DB
        _db.ModelFiles.Remove(entity);
        await _db.SaveChangesAsync(ct);

        // 4) Optionnel: supprimer le dossier si désormais vide
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) &&
                Directory.Exists(dir) &&
                !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
        catch { /* ignorer mais logger si possible */ }

        return true;
    }

    private static ModelFileResponse ToResponse(ModelFile f) => new()
    {
        FileId    = f.FileId,
        Path      = f.Path,
        FileType  = f.FileType,
        FileRole  = f.FileRole,
        IsPrimary = f.IsPrimary,
        Position  = f.Position,
        CreatedAt = f.CreatedAt,
        ModelId   = f.ModelId
    };
}