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
    [HttpGet("overview")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(StatsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsOverviewResponse>> Overview(CancellationToken ct)
    {
        var result = await _svc.GetOverviewAsync(ct);
        return Ok(result);
    }
}
