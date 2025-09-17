using System.Security.Claims;
using System.Text;
using EmbryoApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

public record StudentRegisterRequest(string Email, string Password, string? FirstName, string? LastName);
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
                IsActive  = true // sécurité : on force explicitement
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
                user.IsActive
            });
        });
        
        
        // ✅ /auth/forgot-password → génère un token encodé
        group.MapPost("/forgot-password", async (
            AuthForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.Ok(new { Message = "If account exists, token generated." });

            var rawToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

            // en prod : envoyer par email
            return Results.Ok(new { Email = req.Email, ResetToken = encoded });
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
        

        return group;
    }
}
