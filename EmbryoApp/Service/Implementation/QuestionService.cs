
using EmbryoApp.DTOs;

namespace EmbryoApp.Service.Implementation;

using System.Text.Json;
using EmbryoApp.Data;
using EmbryoApp.Models;
using Microsoft.EntityFrameworkCore;
using EmbryoApp.DTOs.QuestionDtos;
using EmbryoApp.Service.Interface;


public sealed class QuestionService : IQuestionService
{
    private readonly AuthDbContext _db;
    public QuestionService(AuthDbContext db) => _db = db;

    public async Task<PagedResult<QuestionResponse>> ListAsync(QuestionListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = _db.Set<Question>().AsNoTracking()
            .Where(x => x.QuizId == q.QuizId);

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(x => x.QuestionId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new QuestionResponse
            {
                QuestionId = x.QuestionId,
                QuestionType = x.QuestionType.ToString(),
                Statement = x.Statement,
                Options = x.Options,
                CorrectAnswer = x.CorrectAnswer,
                QuizId = x.QuizId
            })
            .ToListAsync(ct);

        return new PagedResult<QuestionResponse> { Total = total, Items = items };
    }

    public async Task<QuestionResponse?> GetByIdAsync(Guid questionId, CancellationToken ct)
    {
        return await _db.Set<Question>().AsNoTracking()
            .Where(x => x.QuestionId == questionId)
            .Select(x => new QuestionResponse
            {
                QuestionId = x.QuestionId,
                QuestionType = x.QuestionType.ToString(),
                Statement = x.Statement,
                Options = x.Options,
                CorrectAnswer = x.CorrectAnswer,
                QuizId = x.QuizId
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(CreateQuestionRequest req, CancellationToken ct)
    {
        // Vérifier Quiz parent
        var quizExists = await _db.Quizzes.AsNoTracking().AnyAsync(z => z.QuizId == req.QuizId, ct);
        if (!quizExists) throw new KeyNotFoundException("quiz_not_found");

        // Validation métier selon QuestionType
        ValidateForType(req.QuestionType, req.Options, req.CorrectAnswer, isUpdate:false);

        var entity = new Question
        {
            QuestionId   = Guid.NewGuid(),
            QuestionType = req.QuestionType,
            Statement    = req.Statement.Trim(),
            Options      = req.Options?.Select(o => o.Trim()).ToList(),
            CorrectAnswer= req.CorrectAnswer,
            QuizId       = req.QuizId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.QuestionId;
    }

    public async Task<QuestionResponse?> UpdateAsync(Guid questionId, UpdateQuestionRequest req, CancellationToken ct)
    {
        var q = await _db.Set<Question>().FirstOrDefaultAsync(x => x.QuestionId == questionId, ct);
        if (q is null) return null;

        // Changer de quiz (optionnel)
        if (req.QuizId.HasValue && req.QuizId.Value != q.QuizId)
        {
            var quizExists = await _db.Quizzes.AsNoTracking().AnyAsync(z => z.QuizId == req.QuizId, ct);
            if (!quizExists) throw new KeyNotFoundException("quiz_not_found");
            q.QuizId = req.QuizId.Value;
        }

        if (req.Statement != null)
            q.Statement = string.IsNullOrWhiteSpace(req.Statement) ? q.Statement : req.Statement.Trim();

        if (req.QuestionType.HasValue)
            q.QuestionType = req.QuestionType.Value;

        if (req.Options != null)
            q.Options = req.Options.Select(o => o.Trim()).ToList();

        if (req.CorrectAnswer != null)
            q.CorrectAnswer = req.CorrectAnswer;

        // Validation (après avoir recalculé l’état cible)
        ValidateForType(q.QuestionType, q.Options, q.CorrectAnswer, isUpdate:true);

        await _db.SaveChangesAsync(ct);

        return new QuestionResponse
        {
            QuestionId = q.QuestionId,
            QuestionType = q.QuestionType.ToString(),
            Statement = q.Statement,
            Options = q.Options,
            CorrectAnswer = q.CorrectAnswer,
            QuizId = q.QuizId
        };
    }

    public async Task<bool> DeleteAsync(Guid questionId, CancellationToken ct)
    {
        var q = await _db.Set<Question>().FirstOrDefaultAsync(x => x.QuestionId == questionId, ct);
        if (q is null) return false;

        _db.Remove(q);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static void ValidateForType(QuestionType type, List<string>? options, string? correct, bool isUpdate)
    {
        switch (type)
        {
            case QuestionType.QCM:
                if (options == null || options.Count < 2)
                    throw new ArgumentException("qcm_options_invalid");
                if (string.IsNullOrWhiteSpace(correct) || !options.Contains(correct))
                    throw new ArgumentException("qcm_answer_invalid");
                break;

            case QuestionType.VraiFaux:
                if (correct is null || (correct.ToLowerInvariant() != "true" && correct.ToLowerInvariant() != "false"))
                    throw new ArgumentException("vf_answer_invalid");
                // options inutiles pour VraiFaux -> on les ignore
                break;

            case QuestionType.Redaction:
                // pas d’exigence stricte; options inutiles
                break;
        }
    }
}
