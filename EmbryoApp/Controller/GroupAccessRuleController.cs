using System.Security.Claims;
using EmbryoApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmbryoApp.Controller;

using EmbryoApp.DTOs.GroupAccessDtos;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EmbryoApp.Data; 

[ApiController]
[Route("api/group-access-rules")]
public sealed class GroupAccessRuleController : ControllerBase
{
    private readonly IGroupAccessRuleService _svc;
    private readonly AuthDbContext _db;
    public GroupAccessRuleController(IGroupAccessRuleService svc, AuthDbContext db)
    {
        _svc = svc;
        _db = db;
    }
    
    
    [HttpGet]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(List<GroupAccessRuleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GroupAccessRuleResponse>>> List([FromQuery] string? groupName, CancellationToken ct)
    {
        var items = await _svc.ListAsync(groupName, ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create([FromBody] CreateGroupAccessRuleRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var name = (req.GroupName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "group_name_required" });

        // On regarde s'il existe déjà une règle pour ce groupe (case-insensitive)
        var existing = await _db.Set<GroupAccessRule>()
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(r => r.GroupName.ToLower() == name.ToLower(), ct);

        var id = await _svc.CreateAsync(req, ct); // upsert dans le service

        if (existing is null)
            return CreatedAtAction(nameof(List), new { id }, new { id, message = "created" });
        else
            return Ok(new { id, message = "updated" });
    }


    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupAccessRuleRequest req, CancellationToken ct)
    {
        var ok = await _svc.UpdateAsync(id, req, ct);
        return ok ? NoContent() : NotFound(new { error = "rule_not_found", id });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Professor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound(new { error = "rule_not_found", id });
    }
    
    // GET /api/group-access-rules/check/me
    [HttpGet("check/me")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(GroupAccessCheckResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GroupAccessCheckResponse>> CheckMine(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // récupère group de l’utilisateur
        var group = _db.Users.Where(u => u.Id == userId).Select(u => u.Group).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(group))
            return Ok(new GroupAccessCheckResponse { HasAccess = false, Message = "Tu n’as pas de groupe assigné." });

        var result = await _svc.CheckAccessAsync(group!, DateTimeOffset.UtcNow, ct);
        return Ok(result);
    }




    
    
}
