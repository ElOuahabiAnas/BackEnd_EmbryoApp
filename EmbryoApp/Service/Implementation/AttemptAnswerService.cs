using EmbryoApp.DTOs.AttemptAnswerDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Service.Implementation;

using EmbryoApp.Data;
using EmbryoApp.Models;
using Microsoft.EntityFrameworkCore;


public sealed class AttemptAnswerService : IAttemptAnswerService
{
    private readonly AuthDbContext _db;
    public AttemptAnswerService(AuthDbContext db) => _db = db;

    public async Task<List<AttemptAnswerResponse>> ListByAttemptAsync(Guid attemptId, CancellationToken ct)
    {
        return await _db.Set<AttemptAnswer>().AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Select(a => new AttemptAnswerResponse
            {
                AttemptId = a.AttemptId,
                QuestionId = a.QuestionId,
                Response = a.Response,
                IsCorrect = a.IsCorrect
            })
            .ToListAsync(ct);
    }

    public async Task<AttemptAnswerResponse?> GetAsync(Guid attemptId, Guid questionId, CancellationToken ct)
    {
        return await _db.Set<AttemptAnswer>().AsNoTracking()
            .Where(a => a.AttemptId == attemptId && a.QuestionId == questionId)
            .Select(a => new AttemptAnswerResponse
            {
                AttemptId = a.AttemptId,
                QuestionId = a.QuestionId,
                Response = a.Response,
                IsCorrect = a.IsCorrect
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Guid attemptId, CreateAttemptAnswerRequest req, CancellationToken ct)
    {
        // Vérifier existence parent Attempt & Question
        var attemptExists = await _db.Attempts.AsNoTracking().AnyAsync(a => a.AttemptId == attemptId, ct);
        if (!attemptExists) throw new KeyNotFoundException("attempt_not_found");

        var questionExists = await _db.Questions.AsNoTracking().AnyAsync(q => q.QuestionId == req.QuestionId, ct);
        if (!questionExists) throw new KeyNotFoundException("question_not_found");

        var entity = new AttemptAnswer
        {
            AttemptId = attemptId,
            QuestionId = req.QuestionId,
            Response = req.Response,
            IsCorrect = req.IsCorrect
        };

        _db.AttemptAnswers.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid attemptId, Guid questionId, CancellationToken ct)
    {
        var entity = await _db.AttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);
        if (entity is null) return false;

        _db.AttemptAnswers.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
