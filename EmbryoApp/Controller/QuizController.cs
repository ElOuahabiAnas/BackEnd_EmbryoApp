using EmbryoApp.DTOs;
using EmbryoApp.DTOs.QuizDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;

// Features/Quizzes/QuizController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/quizzes")]
public sealed class QuizController : ControllerBase
{
    private readonly IQuizService _svc;
    public QuizController(IQuizService svc) => _svc = svc;

    // LIST
    [HttpGet]
    [AllowAnonymous] // ou [Authorize] si tu veux restreindre
    [ProducesResponseType(typeof(PagedResult<QuizResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<QuizResponse>>> List([FromQuery] QuizListQuery q, CancellationToken ct)
        => Ok(await _svc.ListAsync(q, ct));

    // GET by id
    [HttpGet("{quizId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QuizResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizResponse>> Get(Guid quizId, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(quizId, ct);
        return item is null ? NotFound(new { error = "quiz_not_found", quizId }) : Ok(item);
    }

    // CREATE (Professor)
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateQuizRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var id = await _svc.CreateAsync(req, ct);
            return CreatedAtAction(nameof(Get), new { quizId = id }, new { id });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "model_not_found", modelId = req.ModelId });
        }
    }

    // UPDATE (Professor)
    [HttpPut("{quizId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(QuizResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuizResponse>> Update(Guid quizId, [FromBody] UpdateQuizRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var updated = await _svc.UpdateAsync(quizId, req, ct);
            return updated is null ? NotFound(new { error = "quiz_not_found", quizId }) : Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "model_not_found", req.ModelId });
        }
    }

    // DELETE (Professor)
    [HttpDelete("{quizId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid quizId, CancellationToken ct)
        => (await _svc.DeleteAsync(quizId, ct)) ? NoContent() : NotFound(new { error = "quiz_not_found", quizId });
}
