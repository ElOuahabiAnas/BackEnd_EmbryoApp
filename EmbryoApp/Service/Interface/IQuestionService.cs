using EmbryoApp.DTOs.QuestionDtos;
using QuestionResponse = EmbryoApp.DTOs.QuestionDtos.QuestionResponse;
using EmbryoApp.DTOs;

namespace EmbryoApp.Service.Interface;


public interface IQuestionService
{
    Task<DTOs.PagedResult<QuestionResponse>> ListAsync(QuestionListQuery q, CancellationToken ct);
    Task<QuestionResponse?> GetByIdAsync(Guid questionId, CancellationToken ct);
    Task<Guid> CreateAsync(CreateQuestionRequest req, CancellationToken ct);
    Task<QuestionResponse?> UpdateAsync(Guid questionId, UpdateQuestionRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(Guid questionId, CancellationToken ct);
}
