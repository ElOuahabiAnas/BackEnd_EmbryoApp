namespace EmbryoApp.DTOs.StatsDtos;

public sealed class GlobalQuizParticipationStatsResponse
{
    public int ParticipantsCount { get; set; }     // # étudiants distincts ayant tenté ≥1 quiz
    public int TotalStudentsCount { get; set; }    // # étudiants (global ou dans le groupe)
    public double ParticipationRatePercent { get; set; }
}