using EmbryoApp.DTOs.StatsDtos;

namespace EmbryoApp.Service.Interface;


public interface IStatisticsService
{
    Task<StatsOverviewResponse> GetOverviewAsync(CancellationToken ct);
    Task<StudentStatsOverviewResponse> GetStudentOverviewAsync(string userId, CancellationToken ct);
    Task<QuizParticipationStatsResponse> GetQuizParticipationAsync(Guid quizId, string? group, CancellationToken ct);
    Task<GlobalQuizParticipationStatsResponse> GetGlobalQuizParticipationAsync(string? group, CancellationToken ct);

}