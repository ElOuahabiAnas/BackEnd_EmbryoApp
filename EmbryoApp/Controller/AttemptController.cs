using EmbryoApp.DTOs;
using EmbryoApp.DTOs.AttemptDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

// Features/Attempts/AttemptController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/attempts")]
public sealed class AttemptController : ControllerBase
{
    private readonly IAttemptService _svc;
    public AttemptController(IAttemptService svc) => _svc = svc;

    // LIST
    // - Student: on force UserId = token (ignore query.UserId s'il est fourni)
    // - Professor: peut passer UserId et/ou QuizId
    [HttpGet]
    [Authorize] // tout le monde doit être connecté
    [ProducesResponseType(typeof(PagedResult<AttemptResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AttemptResponse>>> List([FromQuery] AttemptListQuery q, CancellationToken ct)
    {
        if (User.IsInRole("Professor"))
        {
            // prof : on respecte les filtres fournis
        }
        else
        {
            // student : on force son propre UserId
            q.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        }

        var result = await _svc.ListAsync(q, ct);
        return Ok(result);
    }

    // GET by id
    // - Student: accès seulement si c'est sa tentative
    // - Professor: accès à tout
    [HttpGet("{attemptId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AttemptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AttemptResponse>> Get(Guid attemptId, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(attemptId, ct);
        if (item is null) return NotFound(new { error = "attempt_not_found", attemptId });

        if (!User.IsInRole("Professor"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (item.UserId != userId) return Forbid();
        }

        return Ok(item);
    }

    // CREATE (Student)
    // UserId vient du token (interdit de le passer dans le body)
    [HttpPost]
    [Authorize(Roles = "Student,Professor")] // autorise aussi un prof à créer (si tu veux), sinon "Student"
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateAttemptRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var id = await _svc.CreateAsync(userId!, req, ct);
            return CreatedAtAction(nameof(Get), new { attemptId = id }, new { id });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "quiz_not_found", quizId = req.QuizId });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.ParamName });
        }
    }

    // DELETE (Professor uniquement) – optionnel
    [HttpDelete("{attemptId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid attemptId, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(attemptId, ct);
        return ok ? NoContent() : NotFound(new { error = "attempt_not_found", attemptId });
    }
    
    // GET stats for connected user
    [HttpGet("my-stats")]
    [Authorize(Roles = "Student,Professor")] // les deux peuvent consulter leurs propres stats
    [ProducesResponseType(typeof(List<AttemptStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AttemptStatsResponse>>> GetMyStats(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var stats = await _svc.GetUserStatsAsync(userId, ct);
        return Ok(stats);
    }
    
    // GET global stats for connected user
    [HttpGet("my-global-stats")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(AttemptGlobalStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttemptGlobalStatsResponse>> GetMyGlobalStats(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var stats = await _svc.GetUserGlobalStatsAsync(userId, ct);
        return Ok(stats);
    }


}
