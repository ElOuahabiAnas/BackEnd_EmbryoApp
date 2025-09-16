using EmbryoApp.DTOs;
using EmbryoApp.DTOs.QuizDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Service.Implementation;
// Features/Quizzes/QuizService.cs
using EmbryoApp.Data;
using EmbryoApp.Models;
using Microsoft.EntityFrameworkCore;


public sealed class QuizService : IQuizService
{
    private readonly AuthDbContext _db;
    public QuizService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<QuizResponse>> ListAsync(QuizListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.Set<Quiz>().AsNoTracking().AsQueryable();

        if (q.ModelId.HasValue) baseQuery = baseQuery.Where(x => x.ModelId == q.ModelId);
        if (q.Status.HasValue)  baseQuery = baseQuery.Where(x => x.Status == q.Status);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.QuizId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new QuizResponse {
                QuizId = x.QuizId,
                Description = x.Description,
                TimeLimit = x.TimeLimit,
                Attempts = x.Attempts,
                Status = x.Status.ToString(),
                PublishedAt = x.PublishedAt,
                ModelId = x.ModelId
            })
            .ToListAsync(ct);

        return new PagedResult<QuizResponse> { Total = total, Items = items };
    }

    public async Task<QuizResponse?> GetByIdAsync(Guid quizId, CancellationToken ct)
    {
        return await _db.Set<Quiz>().AsNoTracking()
            .Where(x => x.QuizId == quizId)
            .Select(x => new QuizResponse {
                QuizId = x.QuizId,
                Description = x.Description,
                TimeLimit = x.TimeLimit,
                Attempts = x.Attempts,
                Status = x.Status.ToString(),
                PublishedAt = x.PublishedAt,
                ModelId = x.ModelId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(CreateQuizRequest req, CancellationToken ct)
    {
        // si ModelId renseigné, on vérifie l’existence du parent
        if (req.ModelId.HasValue)
        {
            var exists = await _db.Models3D.AsNoTracking().AnyAsync(m => m.ModelId == req.ModelId, ct);
            if (!exists) throw new KeyNotFoundException("model_not_found");
        }

        var entity = new Quiz
        {
            QuizId = Guid.NewGuid(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            TimeLimit = req.TimeLimit,
            Attempts = req.Attempts,
            Status = req.Status,
            PublishedAt = req.Status == ModelStatus.Active ? DateTimeOffset.UtcNow : null,
            ModelId = req.ModelId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.QuizId;
    }

    public async Task<QuizResponse?> UpdateAsync(Guid quizId, UpdateQuizRequest req, CancellationToken ct)
    {
        var qz = await _db.Set<Quiz>().FirstOrDefaultAsync(x => x.QuizId == quizId, ct);
        if (qz is null) return null;

        if (req.Description != null) qz.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        if (req.TimeLimit.HasValue)  qz.TimeLimit   = req.TimeLimit;
        if (req.Attempts.HasValue)   qz.Attempts    = req.Attempts;

        if (req.ModelId.HasValue)
        {
            if (req.ModelId.Value == Guid.Empty)
            {
                qz.ModelId = null; // détacher
            }
            else
            {
                var exists = await _db.Models3D.AsNoTracking().AnyAsync(m => m.ModelId == req.ModelId, ct);
                if (!exists) throw new KeyNotFoundException("model_not_found");
                qz.ModelId = req.ModelId;
            }
        }

        if (req.Status.HasValue && req.Status.Value != qz.Status)
        {
            if (req.Status.Value == ModelStatus.Active && qz.PublishedAt is null)
                qz.PublishedAt = DateTimeOffset.UtcNow;

            if (req.Status.Value == ModelStatus.Draft)
                qz.PublishedAt = null;

            qz.Status = req.Status.Value;
        }

        await _db.SaveChangesAsync(ct);

        return new QuizResponse {
            QuizId = qz.QuizId,
            Description = qz.Description,
            TimeLimit = qz.TimeLimit,
            Attempts = qz.Attempts,
            Status = qz.Status.ToString(),
            PublishedAt = qz.PublishedAt,
            ModelId = qz.ModelId
        };
    }

    public async Task<bool> DeleteAsync(Guid quizId, CancellationToken ct)
    {
        var qz = await _db.Set<Quiz>().FirstOrDefaultAsync(x => x.QuizId == quizId, ct);
        if (qz is null) return false;

        _db.Remove(qz);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
