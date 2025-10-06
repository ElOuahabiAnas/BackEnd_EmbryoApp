using System.Security.Claims;
using System.Text;
using EmbryoApp.Models;
using EmbryoApp.Service.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System.Security.Cryptography;
using EmbryoApp.DTOs.AuthDtos;
using EmbryoApp.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


public record StudentRegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? CodeApogee,
    string? CNE,
    string? Group
);


public record AuthForgotPasswordRequest(string Email);
public record AuthResetPasswordRequest(string Email, string Token, string NewPassword);

public record AuthChangePasswordRequest(string CurrentPassword, string NewPassword);






public static class IdentityApiExtensions
{
    public static RouteGroupBuilder MapCustomAuth(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/register", async (
            StudentRegisterRequest req,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILoggerFactory loggerFactory) =>
        {
            var log = loggerFactory.CreateLogger("AuthRegister");

            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = req.FirstName,
                LastName  = req.LastName,
                CodeApogee = req.CodeApogee,
                CNE = req.CNE,
                Group = req.Group,
                IsActive  = true
            };


            var create = await userManager.CreateAsync(user, req.Password);
            if (!create.Succeeded)
            {
                log.LogWarning("Create user failed: {Errors}", string.Join(" | ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
                return Results.BadRequest(create.Errors);
            }

            if (!await roleManager.RoleExistsAsync("Student"))
                return Results.StatusCode(500);

            var addToRole = await userManager.AddToRoleAsync(user, "Student");
            if (!addToRole.Succeeded)
                return Results.BadRequest(addToRole.Errors);

            log.LogInformation("User {Email} registered (Student).", req.Email);
            return Results.Created($"/auth/users/{user.Id}", new {
                Message = "Registered with Student role",
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.CodeApogee,
                user.CNE,
                user.Group,
                user.IsActive
            });

        });
        
        
        // ✅ /auth/forgot-password → génère un token encodé
        group.MapPost("/forgot-password", async (
            AuthForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.Ok(new { Message = "If account exists, email sent." });

            var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            var frontendUrl = config["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/security/reset-password?email={user.Email}&token={encoded}";

            await emailSender.SendAsync(user.Email!, "Password Reset", 
                $"Cliquez sur ce lien pour réinitialiser votre mot de passe : <a href=\"{resetLink}\">{resetLink}</a>");

            return Results.Ok(new { Message = "If account exists, email sent." });
        });

        // ✅ /auth/reset-password → applique le token
        
        group.MapPost("/reset-password", async (
            AuthResetPasswordRequest req,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.BadRequest(new { Error = "User not found" });

            string decoded;
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(req.Token);
                decoded = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return Results.BadRequest(new { Error = "Malformed token" });
            }

            var result = await userManager.ResetPasswordAsync(user, decoded, req.NewPassword);
            if (!result.Succeeded)
                return Results.BadRequest(new { Errors = result.Errors });

            return Results.Ok(new { Message = "Password reset successful" });
        });
        
        group.MapGet("/me", async (
                ClaimsPrincipal principal,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null) return Results.Unauthorized();

                var roles = await userManager.GetRolesAsync(user);

                return Results.Ok(new {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName  = user.LastName,
                    isActive  = user.IsActive,
                    codeApogee = user.CodeApogee,
                    cne = user.CNE,
                    group = user.Group,
                    roles
                });
            })
            .RequireAuthorization();


        
        group.MapPost("/change-password", async (
                ClaimsPrincipal principal,
                AuthChangePasswordRequest req,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.GetUserAsync(principal);
                if (user is null)
                    return Results.Unauthorized();

                var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
                if (!result.Succeeded)
                    return Results.BadRequest(new { Errors = result.Errors });

                return Results.Ok(new { Message = "Password changed successfully" });
            })
            .RequireAuthorization();
        
        
        
group.MapPost("/import-students", async (
    [FromForm] ImportStudentsRequest req,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailSender emailSender,
    ILoggerFactory loggerFactory) =>
{
    var log = loggerFactory.CreateLogger("ImportStudents");

    var file = req.File;
    if (file is null) return Results.BadRequest(new { Error = "No file uploaded" });

    var groupName = Path.GetFileNameWithoutExtension(file.FileName);

    using var stream = file.OpenReadStream();
    using var workbook = new XLWorkbook(stream);
    var worksheet = workbook.Worksheets.First();

    // ✅ Vérifier le rôle une seule fois
    if (!await roleManager.RoleExistsAsync("Student"))
        return Results.StatusCode(500);

    var usersCreated = new List<object>();
    var emailsToSend = new List<(string Email, string FirstName, string Password)>();

    foreach (var row in worksheet.RowsUsed().Skip(1))
    {
        var email = row.Cell(1).GetString().Trim();
        var codeApogee = row.Cell(2).GetString().Trim();
        var cne = row.Cell(3).GetString().Trim();
        var firstName = row.Cell(4).GetString().Trim();
        var lastName = row.Cell(5).GetString().Trim();

        if (string.IsNullOrWhiteSpace(email)) continue;

        var password = PasswordHelper.GenerateSecurePassword();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CodeApogee = codeApogee,
            CNE = cne,
            Group = groupName,
            IsActive = true
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            log.LogWarning("Create user failed: {Email} {Errors}",
                email,
                string.Join(" | ", create.Errors.Select(e => e.Description)));
            continue;
        }

        await userManager.AddToRoleAsync(user, "Student");

        // ✅ Stocke pour envoyer plus tard
        emailsToSend.Add((user.Email!, firstName, password));

        usersCreated.Add(new {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.CodeApogee,
            user.CNE,
            user.Group
        });
    }

    // ✅ Envoi des emails après création de tous les utilisateurs
    foreach (var entry in emailsToSend)
    {
        try
        {
            await emailSender.SendAsync(entry.Email, "Accès à la plateforme",
                $"Bonjour {entry.FirstName},<br/>" +
                $"Votre compte a été créé dans le groupe <b>{groupName}</b>.<br/>" +
                $"Votre mot de passe est : <b>{entry.Password}</b><br/><br/>" +
                $"Merci de le changer après connexion.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Erreur envoi email à {Email}", entry.Email);
        }
    }

    return Results.Ok(new { Message = "Import terminé", Group = groupName, Users = usersCreated });
}).DisableAntiforgery();

group.MapGet("/groups/with-counts", async (UserManager<ApplicationUser> userManager) =>
    {
        var data = await userManager.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.Group))
            .GroupBy(u => u.Group!)
            .Select(g => new { Group = g.Key, Count = g.Count() })
            .OrderBy(x => x.Group)
            .ToListAsync();

        return Results.Ok(data);
    })
    .RequireAuthorization();

// GET /auth/groups/{group}/students?Page=1&PageSize=50&q=search
group.MapGet("/groups/{group}/students", async (
    string group,
    [FromQuery] int? page,
    [FromQuery] int? pageSize,
    [FromQuery] string? q,
    UserManager<ApplicationUser> userManager) =>
{
    // valeurs par défaut si absentes
    var p  = page.GetValueOrDefault(1);
    var ps = pageSize.HasValue
        ? Math.Clamp(pageSize.Value, 1, 200)
        : 50;

    var normGroup = (group ?? string.Empty).Trim();

    // Vérifier si le groupe existe
    var groupExists = await userManager.Users
        .AnyAsync(u => !string.IsNullOrWhiteSpace(u.Group) &&
                       u.Group!.ToLower() == normGroup.ToLower());
    if (!groupExists)
        return Results.NotFound(new { Error = $"Group '{normGroup}' not found" });

    // Récupérer seulement les utilisateurs avec le rôle Student
    var studentsInRole = await userManager.GetUsersInRoleAsync("Student");

    // Filtre par groupe + recherche
    var filtered = studentsInRole
        .Where(u => !string.IsNullOrWhiteSpace(u.Group) &&
                    string.Equals(u.Group!.Trim(), normGroup, StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(q))
    {
        var term = q.Trim();
        filtered = filtered.Where(u =>
            (u.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (u.FirstName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (u.LastName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (u.CodeApogee ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (u.CNE ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
        );
    }

    var total = filtered.Count();

    var items = filtered
        .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Email)
        .Skip((p - 1) * ps)
        .Take(ps)
        .Select(u => new {
            u.Id,
            u.Email,
            u.FirstName,
            u.LastName,
            u.CodeApogee,
            u.CNE,
            Group = u.Group,
            u.IsActive
        })
        .ToList();

    return Results.Ok(new {
        Group = normGroup,
        Page = p,
        PageSize = ps,
        Total = total,
        Items = items
    });
})
.RequireAuthorization(/* ex: "Admin,Professor" */);


// DELETE /auth/students/{id}  → supprime définitivement l'étudiant
group.MapDelete("/students/{id}", async (
        string id,
        UserManager<ApplicationUser> userManager) =>
    {
        // 1) Charger l'utilisateur
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Results.NotFound(new { Error = $"User '{id}' not found." });

        // 2) Vérifier qu'il s'agit bien d'un étudiant (sécurité métier)
        var isStudent = await userManager.IsInRoleAsync(user, "Student");
        if (!isStudent)
            return Results.BadRequest(new { Error = "Only users in role 'Student' can be deleted with this endpoint." });

        
        // 3) Supprimer
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Results.Problem(detail: string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")),
                statusCode: StatusCodes.Status409Conflict);

        return Results.NoContent();
    })
    .RequireAuthorization(/* ex: roles: "Admin,Professor" */);


// DELETE /auth/groups/{group}/students  → supprime définitivement tous les étudiants du groupe
group.MapDelete("/groups/{group}/students", async (
        string group,
        UserManager<ApplicationUser> userManager) =>
    {
        var normGroup = (group ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normGroup))
            return Results.BadRequest(new { Error = "Group name is required." });

        // Récupérer uniquement les utilisateurs ayant le rôle Student
        var allStudents = await userManager.GetUsersInRoleAsync("Student");

        // Filtrer par groupe (case-insensitive)
        var toDelete = allStudents
            .Where(u => !string.IsNullOrWhiteSpace(u.Group) &&
                        string.Equals(u.Group!.Trim(), normGroup, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toDelete.Count == 0)
            return Results.NotFound(new { Error = $"No students found in group '{normGroup}'." });

        var errors = new List<string>();

        foreach (var user in toDelete)
        {
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                errors.Add($"{user.Id}: {string.Join("; ", result.Errors.Select(e => $"{e.Code} {e.Description}"))}");
        }

        // (Optionnel) Si tu veux aussi supprimer la règle d'accès du groupe, voir l'exemple plus bas.

        if (errors.Count > 0)
            return Results.Problem(
                detail: "Some deletions failed: " + string.Join(" | ", errors),
                statusCode: StatusCodes.Status409Conflict);

        return Results.NoContent();
    })
    .RequireAuthorization(/* ex: roles: "Admin,Professor" */);


// DELETE /auth/students  → supprime définitivement tous les étudiants
group.MapDelete("/students", async (UserManager<ApplicationUser> userManager) =>
    {
        var students = await userManager.GetUsersInRoleAsync("Student");
        if (students.Count == 0)
            return Results.NotFound(new { Error = "No students found." });

        var errors = new List<string>();

        foreach (var user in students)
        {
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                errors.Add($"{user.Id}: {string.Join("; ", result.Errors.Select(e => $"{e.Code} {e.Description}"))}");
        }

        if (errors.Count > 0)
            return Results.Problem(
                detail: "Some deletions failed: " + string.Join(" | ", errors),
                statusCode: StatusCodes.Status409Conflict);

        return Results.NoContent();
    })
    .RequireAuthorization(/* ex: roles: "Admin,Professor" */);


        return group;
    }
}
