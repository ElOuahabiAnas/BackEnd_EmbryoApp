using EmbryoApp.DTOs;
using EmbryoApp.DTOs.QuestionDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
public sealed class QuestionController : ControllerBase
{
    private readonly IQuestionService _svc;
    public QuestionController(IQuestionService svc) => _svc = svc;

    // LIST
    [HttpGet("api/quizzes/{quizId:guid}/questions")]
    [AllowAnonymous] // ou [Authorize]
    [ProducesResponseType(typeof(PagedResult<QuestionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<QuestionResponse>>> List(
        Guid quizId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _svc.ListAsync(new QuestionListQuery { QuizId = quizId, Page = page, PageSize = pageSize }, ct);
        return Ok(result);
    }

    // GET by id
    [HttpGet("api/questions/{questionId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionResponse>> Get(Guid questionId, CancellationToken ct)
    {
        var q = await _svc.GetByIdAsync(questionId, ct);
        return q is null ? NotFound(new { error = "question_not_found", questionId }) : Ok(q);
    }

    // CREATE (Professor)
    [HttpPost("api/quizzes/{quizId:guid}/questions")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(Guid quizId, [FromBody] CreateQuestionRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        req.QuizId = quizId; // force la cohérence route/body

        try
        {
            var id = await _svc.CreateAsync(req, ct);
            return CreatedAtAction(nameof(Get), new { questionId = id }, new { id });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "quiz_not_found", quizId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // UPDATE (Professor)
    [HttpPut("api/questions/{questionId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(QuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionResponse>> Update(Guid questionId, [FromBody] UpdateQuestionRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var updated = await _svc.UpdateAsync(questionId, req, ct);
            return updated is null ? NotFound(new { error = "question_not_found", questionId }) : Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { error = "quiz_not_found", req.QuizId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE (Professor)
    [HttpDelete("api/questions/{questionId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid questionId, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(questionId, ct);
        return ok ? NoContent() : NotFound(new { error = "question_not_found", questionId });
    }
}
