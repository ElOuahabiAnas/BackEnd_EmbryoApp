using EmbryoApp.DTOs.AttemptAnswerDtos;

namespace EmbryoApp.Service.Interface;

public interface IAttemptAnswerService
{
    Task<List<AttemptAnswerResponse>> ListByAttemptAsync(Guid attemptId, CancellationToken ct);
    Task<AttemptAnswerResponse?> GetAsync(Guid attemptId, Guid questionId, CancellationToken ct);
    Task AddAsync(Guid attemptId, CreateAttemptAnswerRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid attemptId, Guid questionId, CancellationToken ct);
}