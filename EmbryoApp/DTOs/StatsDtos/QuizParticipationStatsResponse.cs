namespace EmbryoApp.DTOs.StatsDtos;

public sealed class QuizParticipationStatsResponse
{
    public Guid QuizId { get; set; }
    public int ParticipantsCount { get; set; }     // # étudiants distincts avec au moins une tentative
    public int TotalStudentsCount { get; set; }    // # étudiants (global ou dans le groupe)
    public double ParticipationRatePercent { get; set; } // Participants / Total * 100
}