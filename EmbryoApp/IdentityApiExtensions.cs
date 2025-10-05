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



        return group;
    }
}
