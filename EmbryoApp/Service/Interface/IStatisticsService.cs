using EmbryoApp.DTOs.StatsDtos;

namespace EmbryoApp.Service.Interface;


public interface IStatisticsService
{
    Task<StatsOverviewResponse> GetOverviewAsync(CancellationToken ct);
}