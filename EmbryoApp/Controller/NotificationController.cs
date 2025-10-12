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
    public async Task<ActionResult<PagedResult<NotificationResponse>>> List(
        [FromQuery] NotificationListQuery q, CancellationToken ct)
    {
        var isProfessor = User.IsInRole("Professor");
        if (!isProfessor)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            q.UserId = currentUserId; // force userId pour l'étudiant
        }

        return Ok(await _svc.ListAsync(q, isProfessor, ct));
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
    
    // CREATE GLOBAL (Professor only) → crée une notif POUR CHAQUE ÉTUDIANT (UserId renseigné)
    [HttpPost("global")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateGlobal([FromBody] CreateNotificationRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // On ignore req.UserId ici : c’est un broadcast à tous les étudiants
        var created = await _svc.CreateGlobalForAllStudentsAsync(req, ct);

        return Ok(new { created, message = "notifications_created_for_all_students" });
    }

    
    // GET notifications of the connected user
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> ListMine(
        [FromQuery] NotificationListQuery q, CancellationToken ct)
    {
        var isProfessor = User.IsInRole("Professor");
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        // Forcer le UserId sur l'utilisateur courant
        q.UserId = currentUserId;

        return Ok(await _svc.ListAsync(q, isProfessor, ct));
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

    // MARK AS READ (only for the connected user)
    [HttpPost("{id:guid}/read")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        var ok = await _svc.MarkReadAsync(id, currentUserId!, false, ct); // 🚀 false = pas de bypass prof
        if (!ok)
        {
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
    
    
    // POST /api/notifications/group/{group}  → envoie une notif à tous les étudiants du groupe
    [HttpPost("group/{group}")]
    [Authorize(Roles = "Professor,Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateForGroup(string group, [FromBody] CreateNotificationRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(group))
            return BadRequest(new { error = "group_required" });

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var created = await _svc.CreateForGroupAsync(group, req, ct);
        if (created == 0)
            return NotFound(new { error = "no_students_in_group", group });

        return Ok(new { group, created, message = "notifications_created_for_group" });
    }

    


}




