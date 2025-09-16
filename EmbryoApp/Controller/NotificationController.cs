using EmbryoApp.DTOs;
using EmbryoApp.DTOs.NotificationDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

// Features/Notifications/NotificationController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/notifications")]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _svc;
    public NotificationController(INotificationService svc) => _svc = svc;

    // LIST
    // - Student: ne peut lister que les siennes (UserId forcé depuis token)
    // - Professor: peut lister pour n'importe quel UserId (via query)
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> List(
        [FromQuery] NotificationListQuery q, CancellationToken ct)
    {
        var isProfessor = User.IsInRole("Professor");
        if (!isProfessor)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            q.UserId = currentUserId; // force
        }
        return Ok(await _svc.ListAsync(q, ct));
    }

    // GET by id (ownership)
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationResponse>> Get(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(id, ct);
        if (item is null) return NotFound(new { error = "notification_not_found", id });

        var isProfessor = User.IsInRole("Professor");
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!isProfessor && item.UserId != currentUserId) return Forbid();

        return Ok(item);
    }

    // CREATE (Professor)
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateNotificationRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var id = await _svc.CreateAsync(req, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "user_not_found", userId = req.UserId });
        }
    }

    // MARK AS READ (own or elevated)
    [HttpPost("{id:guid}/read")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var isProfessor = User.IsInRole("Professor");

        var ok = await _svc.MarkReadAsync(id, currentUserId!, isProfessor, ct);
        if (!ok)
        {
            // soit not found, soit forbid -> on ne sait pas sans requête en plus
            var item = await _svc.GetByIdAsync(id, ct);
            return item is null ? NotFound(new { error = "notification_not_found", id }) : Forbid();
        }
        return NoContent();
    }

    // MARK ALL AS READ for a user
    [HttpPost("read-all")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead([FromQuery] string? userId, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var isProfessor = User.IsInRole("Professor");

        var target = string.IsNullOrWhiteSpace(userId) ? currentUserId! : userId!;
        var count = await _svc.MarkAllReadAsync(target, currentUserId!, isProfessor, ct);

        return Ok(new { updated = count, userId = target });
    }

    // DELETE (Professor)
    [HttpDelete("{id:guid}")]
[Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound(new { error = "notification_not_found", id });
    }
}
