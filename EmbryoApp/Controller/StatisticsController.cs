using EmbryoApp.DTOs.StatsDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/stats")]
public sealed class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _svc;
    public StatisticsController(IStatisticsService svc) => _svc = svc;

    // Stats globales — généralement réservé aux rôles "Professor" (ou Admin si tu en as un)
    [HttpGet("prof/overview")]
    [Authorize(Roles = "Professor")] // au lieu de Student,Professor
    [ProducesResponseType(typeof(StatsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsOverviewResponse>> Overview(CancellationToken ct)
    {
        var result = await _svc.GetOverviewAsync(ct);
        return Ok(result);
    }
    
    [HttpGet("student/overview")]
    [Authorize(Roles = "Student,Professor")] // ou seulement "Student" si tu préfères
    [ProducesResponseType(typeof(StudentStatsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentStatsOverviewResponse>> StudentOverview(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var result = await _svc.GetStudentOverviewAsync(userId, ct);
        return Ok(result);
    }

    [HttpGet("prof/quizzes/{quizId:guid}/participation")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(QuizParticipationStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizParticipationStatsResponse>> QuizParticipation(
        Guid quizId, [FromQuery] string? group, CancellationToken ct)
    {
        try
        {
            var res = await _svc.GetQuizParticipationAsync(quizId, group, ct);
            return Ok(res);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "quiz_not_found", quizId });
        }
    }

    [HttpGet("prof/quizzes/participation")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(GlobalQuizParticipationStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalQuizParticipationStatsResponse>> GlobalQuizParticipation(
        [FromQuery] string? group, CancellationToken ct)
    {
        var res = await _svc.GetGlobalQuizParticipationAsync(group, ct);
        return Ok(res);
    }

    
}
