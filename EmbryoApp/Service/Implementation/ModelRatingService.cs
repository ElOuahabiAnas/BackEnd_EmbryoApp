namespace EmbryoApp.Service.Implementation;

using EmbryoApp.DTOs.ModelRatingDtos;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using EmbryoApp.Data;
using Microsoft.EntityFrameworkCore;


public sealed class ModelRatingService : IModelRatingService
{
    private readonly AuthDbContext _db;
    public ModelRatingService(AuthDbContext db) => _db = db;

    public async Task<ModelRatingResponse> UpsertAsync(Guid modelId, string userId, int rating, CancellationToken ct)
    {
        var existing = await _db.ModelRatings
            .FirstOrDefaultAsync(r => r.ModelId == modelId && r.UserId == userId, ct);

        if (existing is null)
        {
            var entity = new ModelRating
            {
                ModelRatingId = Guid.NewGuid(),
                ModelId = modelId,
                UserId = userId,
                Rating = rating,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ModelRatings.Add(entity);
            await _db.SaveChangesAsync(ct);

            return new ModelRatingResponse
            {
                ModelRatingId = entity.ModelRatingId,
                ModelId = entity.ModelId,
                UserId = entity.UserId,
                Rating = entity.Rating,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        else
        {
            existing.Rating = rating;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new ModelRatingResponse
            {
                ModelRatingId = existing.ModelRatingId,
                ModelId = existing.ModelId,
                UserId = existing.UserId,
                Rating = existing.Rating,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            };
        }
    }

    public async Task<bool> DeleteMyRatingAsync(Guid modelId, string userId, CancellationToken ct)
    {
        var existing = await _db.ModelRatings
            .FirstOrDefaultAsync(r => r.ModelId == modelId && r.UserId == userId, ct);
        if (existing is null) return false;

        _db.ModelRatings.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ModelRatingSummaryResponse> GetSummaryAsync(Guid modelId, string? currentUserId, CancellationToken ct)
    {
        var query = _db.ModelRatings.AsNoTracking().Where(r => r.ModelId == modelId);

        var count = await query.CountAsync(ct);
        var avg = count == 0 ? 0.0 : Math.Round(await query.AverageAsync(r => r.Rating, ct), 2);

        int? my = null;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            my = await _db.ModelRatings.AsNoTracking()
                .Where(r => r.ModelId == modelId && r.UserId == currentUserId)
                .Select(r => (int?)r.Rating)
                .FirstOrDefaultAsync(ct);
        }

        return new ModelRatingSummaryResponse
        {
            ModelId = modelId,
            Average = avg,
            Count = count,
            MyRating = my
        };
    }

    public async Task<int?> GetMyRatingAsync(Guid modelId, string userId, CancellationToken ct)
    {
        return await _db.ModelRatings.AsNoTracking()
            .Where(r => r.ModelId == modelId && r.UserId == userId)
            .Select(r => (int?)r.Rating)
            .FirstOrDefaultAsync(ct);
    }
}
