namespace EmbryoApp.Controller;

using EmbryoApp.DTOs.ModelRatingDtos;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[ApiController]
[Route("api/models/{modelId:guid}/ratings")]
public sealed class ModelRatingsController : ControllerBase
{
    private readonly IModelRatingService _svc;
    public ModelRatingsController(IModelRatingService svc) => _svc = svc;

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    // GET summary (avg + count + my rating si connecté)
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ModelRatingSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelRatingSummaryResponse>> GetSummary(Guid modelId, CancellationToken ct)
    {
        var res = await _svc.GetSummaryAsync(modelId, CurrentUserId, ct);
        return Ok(res);
    }

    // GET my rating
    [HttpGet("me")]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetMine(Guid modelId, CancellationToken ct)
    {
        var uid = CurrentUserId!;
        var my = await _svc.GetMyRatingAsync(modelId, uid, ct);
        return Ok(new { modelId, myRating = my });
    }

    // POST (create or update) my rating
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(typeof(ModelRatingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ModelRatingResponse>> Upsert(Guid modelId, [FromBody] UpsertModelRatingRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var uid = CurrentUserId!;
        var res = await _svc.UpsertAsync(modelId, uid, req.Rating, ct);
        return Ok(res);
    }

    // DELETE my rating
    [HttpDelete]
    [Authorize(Roles = "Student,Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMine(Guid modelId, CancellationToken ct)
    {
        var uid = CurrentUserId!;
        var ok = await _svc.DeleteMyRatingAsync(modelId, uid, ct);
        return ok ? NoContent() : NotFound(new { error = "rating_not_found", modelId });
    }
}
