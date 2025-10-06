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
                CreatedAt = c.CreatedAt,
                
                // NEW: récupérer la note de l’auteur sur ce modèle (si elle existe)
                UserRating = _db.ModelRatings
                .Where(r => r.ModelId == c.ModelId && r.UserId == c.UserId)
                .Select(r => (int?)r.Rating)
                .FirstOrDefault(),

                ModelRatingId = _db.ModelRatings
                    .Where(r => r.ModelId == c.ModelId && r.UserId == c.UserId)
                    .Select(r => (Guid?)r.ModelRatingId)
                    .FirstOrDefault()
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


    public async Task<(ModelCommentResponse? Response, string? Error)> UpdateAsync(
        Guid modelCommentId,
        string callerUserId,
        UpdateModelCommentRequest req,
        CancellationToken ct)
    {
        var entity = await _db.ModelComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ModelCommentId == modelCommentId, ct);

        if (entity is null)
            return (null, "not_found");

        // Seul l'auteur peut modifier son commentaire (pas même Professor ici)
        if (entity.UserId != callerUserId)
            return (null, "forbidden");

        entity.Content = req.Content.Trim();
        // (Optionnel) si tu ajoutes un champ UpdatedAt dans ModelComment: entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return (new ModelCommentResponse
        {
            ModelCommentId = entity.ModelCommentId,
            ModelId = entity.ModelId,
            UserId = entity.UserId,
            UserFirstName = entity.User?.FirstName,
            UserLastName = entity.User?.LastName,
            UserEmail = entity.User?.Email,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        }, null);
    }
    
    
    public async Task<(IReadOnlyList<ModelCommentResponse> Items, int Total, int Page, int PageSize)> ListMineAsync(
        string userId, MyCommentsQuery query, CancellationToken ct)
    {
        var p  = Math.Max(1, query.Page ?? 1);
        var ps = Math.Clamp(query.PageSize ?? 20, 1, 200);

        var qset = _db.ModelComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.UserId == userId);

        if (query.ModelId is Guid mid && mid != Guid.Empty)
            qset = qset.Where(c => c.ModelId == mid);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            qset = qset.Where(c => c.Content != null && EF.Functions.ILike(c.Content, $"%{term}%"));
            // Si tu n’utilises pas PostgreSQL, remplace par Contains/ToLower() selon ton provider
        }

        var total = await qset.CountAsync(ct);

        var items = await qset
            .OrderByDescending(c => c.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(c => new ModelCommentResponse
            {
                ModelCommentId = c.ModelCommentId,
                ModelId        = c.ModelId,
                UserId         = c.UserId,
                UserFirstName  = c.User!.FirstName,
                UserLastName   = c.User!.LastName,
                UserEmail      = c.User!.Email,
                Content        = c.Content,
                CreatedAt      = c.CreatedAt,
                // + UpdatedAt si tu l’as ajouté
                
                // NEW: rating de l’auteur (l’utilisateur courant) sur CE modèle
                UserRating = _db.ModelRatings
                .Where(r => r.ModelId == c.ModelId && r.UserId == c.UserId)
                .Select(r => (int?)r.Rating)
                .FirstOrDefault(),

                ModelRatingId = _db.ModelRatings
                    .Where(r => r.ModelId == c.ModelId && r.UserId == c.UserId)
                    .Select(r => (Guid?)r.ModelRatingId)
                    .FirstOrDefault()
                
            })
            .ToListAsync(ct);

        return (items, total, p, ps);
    }
}
