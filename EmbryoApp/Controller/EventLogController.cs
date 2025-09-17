using EmbryoApp.DTOs;
using EmbryoApp.DTOs.EventLogDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

using EmbryoApp.DTOs;
using EmbryoApp.DTOs.EventLogDtos;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[ApiController]
[Route("api/event-logs")]
public sealed class EventLogController : ControllerBase
{
    private readonly IEventLogService _svc;
    public EventLogController(IEventLogService svc) => _svc = svc;

    // LIST (Professeurs uniquement)
    [HttpGet]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(PagedResult<EventLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventLogResponse>>> List([FromQuery] EventLogListQuery q, CancellationToken ct)
        => Ok(await _svc.ListAsync(q, ct));

    // GET by id
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(EventLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventLogResponse>> Get(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(id, ct);
        return item is null ? NotFound(new { error = "eventlog_not_found", id }) : Ok(item);
    }

    // CREATE (ouvert à tous les utilisateurs connectés)
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<ActionResult> Create([FromBody] CreateEventLogRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var id = await _svc.CreateAsync(userId, req, ct);

        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }
}
