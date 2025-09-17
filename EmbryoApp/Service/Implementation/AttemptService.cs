using EmbryoApp.DTOs;
using EmbryoApp.DTOs.AttemptDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Service.Implementation;

// Features/Attempts/AttemptService.cs
using EmbryoApp.Data;
using EmbryoApp.Models;
using Microsoft.EntityFrameworkCore;


public sealed class AttemptService : IAttemptService
{
    private readonly AuthDbContext _db;
    public AttemptService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<AttemptResponse>> ListAsync(AttemptListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.Set<Attempt>().AsNoTracking().AsQueryable();

        if (q.QuizId.HasValue)
            baseQuery = baseQuery.Where(a => a.QuizId == q.QuizId);

        if (!string.IsNullOrWhiteSpace(q.UserId))
            baseQuery = baseQuery.Where(a => a.UserId == q.UserId);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(a => a.AttemptedAt)
            .ThenByDescending(a => a.AttemptId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => new AttemptResponse
            {
                AttemptId = a.AttemptId,
                Score = a.Score,
                AttemptedAt = a.AttemptedAt,
                Duration = a.Duration,
                UserId = a.UserId,
                QuizId = a.QuizId
            })
            .ToListAsync(ct);

        return new PagedResult<AttemptResponse> { Total = total, Items = items };
    }

    public async Task<AttemptResponse?> GetByIdAsync(Guid attemptId, CancellationToken ct)
    {
        return await _db.Set<Attempt>().AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Select(a => new AttemptResponse
            {
                AttemptId = a.AttemptId,
                Score = a.Score,
                AttemptedAt = a.AttemptedAt,
                Duration = a.Duration,
                UserId = a.UserId,
                QuizId = a.QuizId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(string userId, CreateAttemptRequest req, CancellationToken ct)
    {
        // Vérifier quiz
        var exists = await _db.Quizzes.AsNoTracking().AnyAsync(q => q.QuizId == req.QuizId, ct);
        if (!exists) throw new KeyNotFoundException("quiz_not_found");

        // Validation simple (déjà côté DTO, mais au cas où)
        if (req.Score < 0 || req.Score > 100) throw new ArgumentOutOfRangeException(nameof(req.Score));
        if (req.Duration <= 0) throw new ArgumentOutOfRangeException(nameof(req.Duration));

        var entity = new Attempt
        {
            AttemptId = Guid.NewGuid(),
            Score = decimal.Round(req.Score, 2),
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = req.Duration,
            UserId = userId,
            QuizId = req.QuizId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.AttemptId;
    }

    public async Task<bool> DeleteAsync(Guid attemptId, CancellationToken ct)
    {
        var a = await _db.Set<Attempt>().FirstOrDefaultAsync(x => x.AttemptId == attemptId, ct);
        if (a is null) return false;

        _db.Remove(a);
        await _db.SaveChangesAsync(ct);
        return true;
    }
    
    public async Task<List<AttemptStatsResponse>> GetUserStatsAsync(string userId, CancellationToken ct)
    {
        return await _db.Attempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.QuizId)
            .Select(g => new AttemptStatsResponse
            {
                QuizId = g.Key,
                AttemptCount = g.Count(),
                AverageScore = g.Average(x => x.Score)
            })
            .ToListAsync(ct);
    }
    
    public async Task<AttemptGlobalStatsResponse> GetUserGlobalStatsAsync(string userId, CancellationToken ct)
    {
        var query = _db.Attempts.AsNoTracking().Where(a => a.UserId == userId);

        var total = await query.CountAsync(ct);
        var avg = total > 0 ? await query.AverageAsync(a => a.Score, ct) : 0;

        return new AttemptGlobalStatsResponse
        {
            TotalAttempts = total,
            GlobalAverageScore = decimal.Round((decimal)avg, 2)
        };
    }

    
    

}
