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
    
    
    private static string GenerateAlphaNumPassword(int length = 10)
    {
        if (length < 3) throw new ArgumentOutOfRangeException(nameof(length), "length must be >= 3");

        const string U = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string L = "abcdefghijklmnopqrstuvwxyz";
        const string D = "0123456789";
        const string ALL = U + L + D;

        var pwd = new char[length];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        // Garantir 1 majuscule, 1 minuscule, 1 chiffre
        pwd[0] = U[GetUnbiasedIndex(rng, U.Length)];
        pwd[1] = L[GetUnbiasedIndex(rng, L.Length)];
        pwd[2] = D[GetUnbiasedIndex(rng, D.Length)];

        // Le reste aléatoire parmi ALL
        for (int i = 3; i < length; i++)
            pwd[i] = ALL[GetUnbiasedIndex(rng, ALL.Length)];

        // Shuffle Fisher-Yates
        for (int i = length - 1; i > 0; i--)
        {
            int j = GetUnbiasedIndex(rng, i + 1);
            (pwd[i], pwd[j]) = (pwd[j], pwd[i]);
        }
        return new string(pwd);
    }

    private static int GetUnbiasedIndex(System.Security.Cryptography.RandomNumberGenerator rng, int maxExclusive)
    {
        // tirage uniforme sans biais modulo
        var upperBound = (uint.MaxValue / (uint)maxExclusive) * (uint)maxExclusive;
        Span<byte> b = stackalloc byte[4];
        uint r;
        do
        {
            rng.GetBytes(b);
            r = BitConverter.ToUInt32(b);
        } while (r >= upperBound);

        return (int)(r % (uint)maxExclusive);
    }

    
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

    // Nom de groupe par défaut = nom du fichier (fallback si pas de colonne Group)
    var defaultGroupName = Path.GetFileNameWithoutExtension(file.FileName);

    using var stream = file.OpenReadStream();
    using var workbook = new XLWorkbook(stream);
    var worksheet = workbook.Worksheets.First();

    // Vérifier le rôle Student une seule fois
    if (!await roleManager.RoleExistsAsync("Student"))
        return Results.StatusCode(500);

    // ---------- Lecture de l'en-tête & construction de la map -----------
    var headerRow = worksheet.FirstRowUsed();
    if (headerRow is null)
        return Results.BadRequest(new { Error = "Empty worksheet" });

    // Normalisation: minuscules, trim, retirer espaces/accents/punctuations simples
    string Norm(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim().ToLowerInvariant();

        // enlever accents de base
        var normalized = t.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);
        foreach (var ch in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        t = sb.ToString().Normalize(NormalizationForm.FormC);

        // enlever espaces, _ et - pour matcher "code apogee", "code_apogee", etc.
        t = t.Replace(" ", "").Replace("_", "").Replace("-", "");
        return t;
    }

    // Map header normalisé -> index de colonne
    var headerMap = new Dictionary<string, int>();
    foreach (var cell in headerRow.CellsUsed())
    {
        var key = Norm(cell.GetString());
        if (!headerMap.ContainsKey(key))
            headerMap[key] = cell.Address.ColumnNumber;
    }

    // Helper: récupérer l’index d’une colonne via une liste d’alias normalisés
    int? GetCol(params string[] aliases)
    {
        foreach (var a in aliases)
        {
            var key = Norm(a);
            if (headerMap.TryGetValue(key, out var idx))
                return idx;
        }
        return null;
    }

    // Colonnes recherchées (avec alias)
    var colEmail      = GetCol("email", "e-mail", "courriel", "mail");
    var colCne        = GetCol("cne");
    var colCodeApogee = GetCol("codeapogee", "codeapogee", "codeapogee", "code apogee", "code apogée");
    var colFirstName  = GetCol("firstname", "first name", "prenom", "prénom");
    var colLastName   = GetCol("lastname", "last name", "nom");
    var colGroup      = GetCol("group", "groupe");

    if (colEmail is null)
        return Results.BadRequest(new { Error = "Required column 'Email' not found" });

    var usersCreated = new List<object>();
    var emailsToSend = new List<(string Email, string FirstName, string Password)>();

    // ---------- Parcours des lignes de données -----------
    foreach (var row in worksheet.RowsUsed().Skip(1)) // skip header
    {
        string GetStr(int? col) => (col is null) ? "" : row.Cell(col.Value).GetString().Trim();

        var email     = GetStr(colEmail);
        var codeApogee= GetStr(colCodeApogee);
        var cne       = GetStr(colCne);
        var firstName = GetStr(colFirstName);
        var lastName  = GetStr(colLastName);
        var groupName = GetStr(colGroup);

        if (string.IsNullOrWhiteSpace(email))
            continue; // ligne vide ou invalide

        if (string.IsNullOrWhiteSpace(groupName))
            groupName = defaultGroupName; // fallback: nom du fichier

        var password = GenerateAlphaNumPassword(10);

        var user = new ApplicationUser
        {
            UserName   = email,
            Email      = email,
            FirstName  = string.IsNullOrWhiteSpace(firstName) ? null : firstName,
            LastName   = string.IsNullOrWhiteSpace(lastName)  ? null : lastName,
            CodeApogee = string.IsNullOrWhiteSpace(codeApogee) ? null : codeApogee,
            CNE        = string.IsNullOrWhiteSpace(cne) ? null : cne,
            Group      = string.IsNullOrWhiteSpace(groupName) ? null : groupName,
            IsActive   = true
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            // log et passer à la ligne suivante
            log.LogWarning("Create user failed: {Email} {Errors}",
                email,
                string.Join(" | ", create.Errors.Select(e => e.Description)));
            continue;
        }

        await userManager.AddToRoleAsync(user, "Student");

        emailsToSend.Add((user.Email!, user.FirstName ?? "", password));

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

    // Envoi des emails après les créations
    foreach (var entry in emailsToSend)
    {
        try
        {
            await emailSender.SendAsync(entry.Email, "Accès à la plateforme",
                $"Bonjour {entry.FirstName},<br/>" +
                $"Votre compte a été créé dans le groupe <b>{(string.IsNullOrWhiteSpace(defaultGroupName) ? "N/A" : defaultGroupName)}</b>.<br/>" +
                $"Votre mot de passe est : <b>{entry.Password}</b><br/><br/>" +
                $"Merci de le changer après connexion.");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Erreur envoi email à {Email}", entry.Email);
        }
    }

    return Results.Ok(new { Message = "Import terminé", Users = usersCreated });
})
.DisableAntiforgery();

// GET /auth/groups/with-counts?groupName=GI3
group.MapGet("/groups/with-counts", async (
        [FromQuery] string? groupName,
        UserManager<ApplicationUser> userManager) =>
    {
        var query = userManager.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.Group));

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var term = groupName.Trim();
            query = query.Where(u => EF.Functions.ILike(u.Group!, $"%{term}%"));
        }


        var data = await query
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
        .OrderBy(u => u.Email) // tri alphabétique par email A → Z
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


// DELETE /auth/groups/{group}/students  → supprime tous les étudiants du groupe
// et supprime aussi la/les GroupAccessRule correspondantes
group.MapDelete("/groups/{group}/students", async (
        string group,
        UserManager<ApplicationUser> userManager,
        EmbryoApp.Data.AuthDbContext db) =>
{
    var normGroup = (group ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(normGroup))
        return Results.BadRequest(new { Error = "Group name is required." });

    // Transaction pour garder la cohérence (suppression users + rules)
    await using var tx = await db.Database.BeginTransactionAsync();

    // 1) Récupérer les étudiants (rôle Student) du groupe (case-insensitive)
    var allStudents = await userManager.GetUsersInRoleAsync("Student"); // Identity store = AuthDbContext
    var toDelete = allStudents
        .Where(u => !string.IsNullOrWhiteSpace(u.Group)
                 && string.Equals(u.Group!.Trim(), normGroup, StringComparison.OrdinalIgnoreCase))
        .ToList();

    var errors = new List<string>();
    foreach (var user in toDelete)
    {
        var res = await userManager.DeleteAsync(user);
        if (!res.Succeeded)
            errors.Add($"{user.Id}: {string.Join("; ", res.Errors.Select(e => $"{e.Code} {e.Description}"))}");
    }

    if (errors.Count > 0)
    {
        await tx.RollbackAsync();
        return Results.Problem(
            detail: "Some deletions failed: " + string.Join(" | ", errors),
            statusCode: StatusCodes.Status409Conflict);
    }

    // 2) Supprimer la/les règles d’accès du groupe (case-insensitive)
    var rules = await db.Set<GroupAccessRule>()
        .Where(r => r.GroupName.ToLower() == normGroup.ToLower())
        .ToListAsync();

    if (rules.Count > 0)
    {
        db.RemoveRange(rules);
        await db.SaveChangesAsync();
    }

    await tx.CommitAsync();

    // Retour clair selon ce qui a été supprimé
    return Results.Ok(new
    {
        Group = normGroup,
        StudentsDeleted = toDelete.Count,
        RulesDeleted = rules.Count
    });
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


// POST /auth/register-auto  → crée un étudiant avec mot de passe auto-généré et envoie par email
group.MapPost("/register-auto", async (
    StudentRegisterAutoRequest req,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IEmailSender emailSender,
    ILoggerFactory loggerFactory) =>
{
    var log = loggerFactory.CreateLogger("AuthRegisterAuto");

    // 1) existant ?
    var existing = await userManager.FindByEmailAsync(req.Email);
    if (existing is not null)
        return Results.Conflict(new { Error = "email_already_exists" });

    // 2) user
    var user = new ApplicationUser
    {
        UserName   = req.Email,
        Email      = req.Email,
        FirstName  = req.FirstName,
        LastName   = req.LastName,
        CodeApogee = req.CodeApogee,
        CNE        = req.CNE,
        Group      = req.Group,
        IsActive   = true
    };

    // 3) password auto (même helper que /auth/import-students)
    var password = GenerateAlphaNumPassword(10);

    var create = await userManager.CreateAsync(user, password);
    if (!create.Succeeded)
    {
        log.LogWarning("Create user failed: {Errors}",
            string.Join(" | ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
        return Results.BadRequest(create.Errors);
    }

    // 4) rôle Student requis
    if (!await roleManager.RoleExistsAsync("Student"))
        return Results.StatusCode(500);

    var addToRole = await userManager.AddToRoleAsync(user, "Student");
    if (!addToRole.Succeeded)
        return Results.BadRequest(addToRole.Errors);

    // 5) envoi email (même pattern que forgot/import)
    try
    {
        await emailSender.SendAsync(user.Email!, "Accès à la plateforme",
            $"Bonjour {user.FirstName ?? ""},<br/>" +
            $"Votre compte a été créé.<br/>" +
            $"Votre mot de passe temporaire est : <b>{password}</b><br/><br/>" +
            $"Par sécurité, changez-le après connexion dans votre espace.");
    }
    catch (Exception ex)
    {
        // Selon ta politique, tu peux décider de supprimer l'utilisateur si mail échoue
        log.LogError(ex, "Erreur envoi email à {Email}", user.Email);
        // return Results.Problem("email_send_failed"); // si tu préfères échouer
    }

    log.LogInformation("User {Email} registered (Student) with auto password.", req.Email);

    return Results.Created($"/auth/users/{user.Id}", new {
        Message = "Registered with Student role (password emailed)",
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.CodeApogee,
        user.CNE,
        user.Group,
        user.IsActive
        // ⚠️ On ne renvoie PAS le mot de passe en réponse API.
    });
});

// PUT /auth/me  → met à jour le profil de l’utilisateur connecté (champs simples)
group.MapPut("/me", async (
        ClaimsPrincipal principal,
        UpdateMyProfileRequest req,
        UserManager<ApplicationUser> userManager) =>
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null) return Results.Unauthorized();

        // (Optionnel) petites validations côté API
        static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var newFirstName  = TrimOrNull(req.FirstName);
        var newLastName   = TrimOrNull(req.LastName);
        var newCodeApogee = TrimOrNull(req.CodeApogee);
        var newCne        = TrimOrNull(req.CNE);

        // Appliquer uniquement si fourni (permet les partial updates)
        if (req.FirstName  != null) user.FirstName  = newFirstName;
        if (req.LastName   != null) user.LastName   = newLastName;
        if (req.CodeApogee != null) user.CodeApogee = newCodeApogee;
        if (req.CNE        != null) user.CNE        = newCne;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Results.BadRequest(new { Errors = result.Errors });

        return Results.Ok(new {
            Message = "Profile updated",
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName  = user.LastName,
            codeApogee = user.CodeApogee,
            cne = user.CNE,
            group = user.Group,
            isActive = user.IsActive
        });
    })
    .RequireAuthorization();

// PUT /auth/students/{id} → Prof/Admin met à jour les données d’un étudiant (incl. Group)
group.MapPut("/students/{id}", async (
        string id,
        UpdateStudentByIdRequest req,
        UserManager<ApplicationUser> userManager) =>
    {
        // 1) Charger l’utilisateur
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return Results.NotFound(new { Error = $"User '{id}' not found." });

        // 2) Vérifier qu’il s’agit d’un étudiant
        var isStudent = await userManager.IsInRoleAsync(user, "Student");
        if (!isStudent)
            return Results.BadRequest(new { Error = "Only users in role 'Student' can be updated with this endpoint." });

        // 3) Helpers
        static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // 4) Appliquer uniquement les champs fournis (partial update)
        if (req.FirstName  is not null) user.FirstName  = TrimOrNull(req.FirstName);
        if (req.LastName   is not null) user.LastName   = TrimOrNull(req.LastName);
        if (req.CodeApogee is not null) user.CodeApogee = TrimOrNull(req.CodeApogee);
        if (req.CNE        is not null) user.CNE        = TrimOrNull(req.CNE);
        if (req.Group      is not null) user.Group      = TrimOrNull(req.Group); // professeur peut modifier le groupe

        // 5) Persister
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Results.BadRequest(new { Errors = result.Errors });

        // 6) Réponse
        return Results.Ok(new {
            Message = "Student updated",
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName  = user.LastName,
            codeApogee = user.CodeApogee,
            cne = user.CNE,
            group = user.Group,
            isActive = user.IsActive
        });
    })
    .RequireAuthorization(/* ex: roles: "Professor,Admin" */);

        return group;
    }
}
