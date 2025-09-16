using EmbryoApp.DTOs;
using EmbryoApp.DTOs.QuizDtos;

namespace EmbryoApp.Service.Interface;

public interface IQuizService
{
    Task<PagedResult<QuizResponse>> ListAsync(QuizListQuery q, CancellationToken ct);
    Task<QuizResponse?> GetByIdAsync(Guid quizId, CancellationToken ct);
    Task<Guid> CreateAsync(CreateQuizRequest req, CancellationToken ct);
    Task<QuizResponse?> UpdateAsync(Guid quizId, UpdateQuizRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid quizId, CancellationToken ct);
}