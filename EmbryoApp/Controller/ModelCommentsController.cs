namespace EmbryoApp.Controller;

using EmbryoApp.DTOs;
using EmbryoApp.DTOs.ModelCommentDtos;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[ApiController]
[Route("api/models/{modelId:guid}/comments")]
public sealed class ModelCommentsController : ControllerBase
{
    private readonly IModelCommentService _svc;
    public ModelCommentsController(IModelCommentService svc) => _svc = svc;

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    // GET list paginée des commentaires
    [HttpGet]
    [AllowAnonymous] // ou [Authorize] si tu veux restreindre
    [ProducesResponseType(typeof(PagedResult<ModelCommentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ModelCommentResponse>>> List(Guid modelId, [FromQuery] ModelCommentListQuery q, CancellationToken ct)
    {
        var res = await _svc.ListAsync(modelId, q, ct);
        return Ok(res);
    }

    // POST créer un commentaire (user connecté)
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(ModelCommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModelCommentResponse>> Create(Guid modelId, [FromBody] CreateModelCommentRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var uid = CurrentUserId!;
        var res = await _svc.CreateAsync(modelId, uid, req, ct);
        if (res is null) return NotFound(new { error = "model_not_found", modelId });
        return CreatedAtAction(nameof(List), new { modelId }, res);
    }

    // DELETE supprimer un commentaire (auteur ou prof)
    [HttpDelete("{commentId:guid}")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid modelId, Guid commentId, CancellationToken ct)
    {
        var uid = CurrentUserId!;
        var isProfessor = User.IsInRole("Professor");
        var ok = await _svc.DeleteAsync(commentId, uid, isProfessor, ct);
        if (!ok) return NotFound(new { error = "comment_not_found_or_forbidden", modelId, commentId });
        return NoContent();
    }

    [HttpPut("{commentId:guid}")]
    [Authorize(Roles =
        "Student,Professor")] // l’auteur doit juste être authentifié; le rôle prof n’accorde pas l’édition
    [ProducesResponseType(typeof(ModelCommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelCommentResponse>> Update(Guid modelId, Guid commentId,
        [FromBody] UpdateModelCommentRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var uid = CurrentUserId!;
        var (res, err) = await _svc.UpdateAsync(commentId, uid, req, ct);

        if (err == "not_found")
            return NotFound(new { error = "comment_not_found", modelId, commentId });

        if (err == "forbidden")
            return Forbid(); // 403

        return Ok(res);
    }

    [Authorize(Roles = "Student,Professor")]
    [HttpGet("~/api/comments/me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetMyComments([FromQuery] MyCommentsQuery query, CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!; // ou ta propriété CurrentUserId

        var (items, total, page, pageSize) = await _svc.ListMineAsync(uid, query, ct);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }


}
