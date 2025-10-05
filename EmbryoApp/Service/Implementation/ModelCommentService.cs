namespace EmbryoApp.Service.Implementation;

using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelCommentDtos;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using EmbryoApp.Data;
using Microsoft.EntityFrameworkCore;


public sealed class ModelCommentService : IModelCommentService
{
    private readonly AuthDbContext _db;
    public ModelCommentService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<ModelCommentResponse>> ListAsync(Guid modelId, ModelCommentListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.ModelComments
            .AsNoTracking()
            .Where(c => c.ModelId == modelId);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new ModelCommentResponse
            {
                ModelCommentId = c.ModelCommentId,
                ModelId = c.ModelId,
                UserId = c.UserId,
                UserFirstName = c.User!.FirstName,
                UserLastName = c.User!.LastName,
                UserEmail = c.User!.Email,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<ModelCommentResponse> { Total = total, Items = items };
    }

    public async Task<ModelCommentResponse?> CreateAsync(Guid modelId, string userId, CreateModelCommentRequest req, CancellationToken ct)
    {
        // (optionnel) vérifier que le modèle existe :
        // var exists = await _db.Set<Model3D>().AnyAsync(m => m.ModelId == modelId, ct);
        // if (!exists) return null;

        var entity = new ModelComment
        {
            ModelCommentId = Guid.NewGuid(),
            ModelId = modelId,
            UserId = userId,
            Content = req.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ModelComments.Add(entity);
        await _db.SaveChangesAsync(ct);

        // retourner avec infos user
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(ct);

        return new ModelCommentResponse
        {
            ModelCommentId = entity.ModelCommentId,
            ModelId = entity.ModelId,
            UserId = entity.UserId,
            UserFirstName = user?.FirstName,
            UserLastName = user?.LastName,
            UserEmail = user?.Email,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid modelCommentId, string callerUserId, bool isProfessor, CancellationToken ct)
    {
        var entity = await _db.ModelComments.FirstOrDefaultAsync(c => c.ModelCommentId == modelCommentId, ct);
        if (entity is null) return false;

        // Seul l'auteur ou un professeur peut supprimer
        if (!isProfessor && entity.UserId != callerUserId) return false;

        _db.ModelComments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
