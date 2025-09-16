using EmbryoApp.DTOs.Model3D;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Controller;
// Features/Models3D/Model3DController.cs
using System.Security.Claims;
using EmbryoApp;
using EmbryoApp.DTOs;
using EmbryoApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/models")]
public sealed class Model3DController : ControllerBase
{
    private readonly IModel3DService _svc;

    public Model3DController(IModel3DService svc) => _svc = svc;

    // READ: Students & Professors (auth requis selon ton besoin; ici on exige auth)
    [HttpGet]
    [Authorize] // si tu veux ouvert à tous, remplace par [AllowAnonymous]
    public async Task<ActionResult<PagedResult<Model3DResponse>>> List(
        [FromQuery] Model3DListQuery q,
        CancellationToken ct)
    {
        var result = await _svc.ListAsync(q, ct);
        return Ok(result);
    }

    // READ: by id
    [HttpGet("{id:guid}")]
    [AllowAnonymous] // ou [Authorize] si tu veux restreindre
    [ProducesResponseType(typeof(Model3DResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Model3DResponse>> Get(Guid id, CancellationToken ct)
    {
        var item = await _svc.GetByIdAsync(id, ct);
        if (item is null)
            return NotFound(new { error = "model_not_found", id });

        return Ok(item);
    }

    // CREATE: Professor only
    [HttpPost]
    [Authorize(Roles = "Student,Professor")]
    public async Task<ActionResult> Create([FromBody] CreateModel3DRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Récupérer l'ID utilisateur depuis le token
        var authorUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)      // classique
            ?? User.FindFirstValue("sub");                      // fallback si le token expose "sub"

        if (string.IsNullOrEmpty(authorUserId)) return Unauthorized();

        var id = await _svc.CreateAsync(authorUserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    // UPDATE: Professor only
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Student,Professor")]
    public async Task<ActionResult<Model3DResponse>> Update(Guid id, [FromBody] UpdateModel3DRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await _svc.UpdateAsync(id, req, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE: Professor only
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Student,Professor")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
