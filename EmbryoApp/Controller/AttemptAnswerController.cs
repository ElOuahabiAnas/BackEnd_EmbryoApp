using EmbryoApp.DTOs.AttemptAnswerDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

// Features/AttemptAnswers/AttemptAnswerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/attempts/{attemptId:guid}/answers")]
public sealed class AttemptAnswerController : ControllerBase
{
    private readonly IAttemptAnswerService _svc;
    public AttemptAnswerController(IAttemptAnswerService svc) => _svc = svc;

    // LIST by attempt
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<AttemptAnswerResponse>>> List(Guid attemptId, CancellationToken ct)
        => Ok(await _svc.ListByAttemptAsync(attemptId, ct));

    // GET by composite key
    [HttpGet("{questionId:guid}")]
    [Authorize]
    public async Task<ActionResult<AttemptAnswerResponse>> Get(Guid attemptId, Guid questionId, CancellationToken ct)
    {
        var ans = await _svc.GetAsync(attemptId, questionId, ct);
        return ans is null ? NotFound(new { error = "not_found", attemptId, questionId }) : Ok(ans);
    }

    // ADD answer (Student)
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    public async Task<ActionResult> Add(Guid attemptId, [FromBody] CreateAttemptAnswerRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            await _svc.AddAsync(attemptId, req, ct);
            return CreatedAtAction(nameof(Get), new { attemptId, questionId = req.QuestionId }, new { attemptId, questionId = req.QuestionId });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE (Professor)
    [HttpDelete("{questionId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    public async Task<IActionResult> Delete(Guid attemptId, Guid questionId, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(attemptId, questionId, ct);
        return ok ? NoContent() : NotFound(new { error = "not_found", attemptId, questionId });
    }
}
