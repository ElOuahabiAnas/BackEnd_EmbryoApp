namespace EmbryoApp.DTOs.AttemptDtos;

public sealed class AttemptGlobalStatsResponse
{
    public int TotalAttempts { get; set; }
    public decimal GlobalAverageScore { get; set; }
}