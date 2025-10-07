using EmbryoApp.DTOs.StatsDtos;
using EmbryoApp.Service.Interface;

namespace EmbryoApp.Service.Implementation;

// Features/Statistics/StatisticsService.cs
using EmbryoApp.Data;
using Microsoft.EntityFrameworkCore;
using EmbryoApp.Models;


public sealed class StatisticsService : IStatisticsService
{
    private readonly AuthDbContext _db;
    public StatisticsService(AuthDbContext db) => _db = db;

    public async Task<StatsOverviewResponse> GetOverviewAsync(CancellationToken ct)
    {
        var modelsCount  = await _db.Models3D.CountAsync(ct);
        var quizzesCount = await _db.Quizzes.CountAsync(ct);

        // Étudiants (comme avant)
        var studentRoleId = await _db.Roles
            .Where(r => r.NormalizedName == "STUDENT")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var studentsCount = 0;
        if (!string.IsNullOrEmpty(studentRoleId))
        {
            studentsCount = await _db.UserRoles
                .Where(ur => ur.RoleId == studentRoleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .CountAsync(ct);
        }

        // NEW: nombre de groupes existants (distincts, non vides)
        var groupsCount = await _db.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.Group))
            .Select(u => u.Group!)
            .Distinct()
            .CountAsync(ct);

        return new StatsOverviewResponse
        {
            ModelsCount   = modelsCount,
            QuizzesCount  = quizzesCount,
            StudentsCount = studentsCount,
            GroupsCount   = groupsCount
        };
    }

    
    public async Task<StudentStatsOverviewResponse> GetStudentOverviewAsync(string userId, CancellationToken ct)
    {
        // 1) Comptes "Active"
        var activeModelsCount = await _db.Models3D
            .CountAsync(m => m.Status == ModelStatus.Active, ct); // Model3D.Status Active :contentReference[oaicite:4]{index=4} :contentReference[oaicite:5]{index=5}

        var activeQuizzesCount = await _db.Quizzes
            .CountAsync(q => q.Status == ModelStatus.Active, ct); // Quiz.Status Active :contentReference[oaicite:6]{index=6} :contentReference[oaicite:7]{index=7}

        // 2) Groupe de l'utilisateur connecté
        var myGroup = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Group)
            .FirstOrDefaultAsync(ct); // ApplicationUser.Group :contentReference[oaicite:8]{index=8}

        var groupStudentsCount = 0;

        if (!string.IsNullOrWhiteSpace(myGroup))
        {
            // id du rôle STUDENT
            var studentRoleId = await _db.Roles
                .Where(r => r.NormalizedName == "STUDENT")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrEmpty(studentRoleId))
            {
                // Étudiants du même groupe (case-insensitive)
                var normGroup = myGroup.Trim().ToLower();

                groupStudentsCount = await (
                    from u in _db.Users
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    where ur.RoleId == studentRoleId
                          && u.Group != null
                          && u.Group.ToLower() == normGroup
                    select u.Id
                ).Distinct().CountAsync(ct);
            }
        }

        return new StudentStatsOverviewResponse
        {
            ActiveModelsCount  = activeModelsCount,
            ActiveQuizzesCount = activeQuizzesCount,
            GroupStudentsCount = groupStudentsCount
        };
    }
    
    
    public async Task<QuizParticipationStatsResponse> GetQuizParticipationAsync(
    Guid quizId, string? group, CancellationToken ct)
{
    // 0) Vérifier que le quiz existe (optionnel mais propre)
    var quizExists = await _db.Quizzes.AnyAsync(q => q.QuizId == quizId, ct); // Quiz entity
    if (!quizExists) throw new KeyNotFoundException("quiz_not_found"); // ou renvoie null et 404 au controller

    // 1) Rôle STUDENT
    var studentRoleId = await _db.Roles
        .Where(r => r.NormalizedName == "STUDENT")
        .Select(r => r.Id)
        .FirstOrDefaultAsync(ct);

    // 2) Ensemble des étudiants (global ou restreint par groupe)
    IQueryable<string> studentIdsQ = from u in _db.Users
                                     join ur in _db.UserRoles on u.Id equals ur.UserId
                                     where ur.RoleId == studentRoleId
                                     select u.Id;

    if (!string.IsNullOrWhiteSpace(group))
    {
        var g = group.Trim().ToLower();
        studentIdsQ = from u in _db.Users
                      join ur in _db.UserRoles on u.Id equals ur.UserId
                      where ur.RoleId == studentRoleId
                         && u.Group != null
                         && u.Group.ToLower() == g
                      select u.Id;
    }

    var totalStudentsCount = await studentIdsQ.Distinct().CountAsync(ct);

    // 3) Participants = étudiants distincts ayant au moins une tentative sur ce quiz
    //    (filtrés par le même périmètre: global ou groupe)
    var participantsQ = from a in _db.Attempts // Attempt has UserId, QuizId
                        where a.QuizId == quizId
                        select a.UserId;

    // Si filtre de groupe, on restreint aux étudiants du groupe
    if (!string.IsNullOrWhiteSpace(group))
    {
        var g = group.Trim().ToLower();
        participantsQ =
            from a in _db.Attempts
            join u in _db.Users on a.UserId equals u.Id
            join ur in _db.UserRoles on u.Id equals ur.UserId
            where a.QuizId == quizId
              && ur.RoleId == studentRoleId
              && u.Group != null
              && u.Group.ToLower() == g
            select a.UserId;
    }
    else
    {
        // global: restreindre aux étudiants pour être cohérent
        participantsQ =
            from a in _db.Attempts
            join ur in _db.UserRoles on a.UserId equals ur.UserId
            where a.QuizId == quizId
              && ur.RoleId == studentRoleId
            select a.UserId;
    }

    var participantsCount = await participantsQ.Distinct().CountAsync(ct);

    var rate = totalStudentsCount == 0 ? 0.0
             : Math.Round((participantsCount * 100.0) / totalStudentsCount, 2);

    return new QuizParticipationStatsResponse
    {
        QuizId = quizId,
        ParticipantsCount = participantsCount,
        TotalStudentsCount = totalStudentsCount,
        ParticipationRatePercent = rate
    };
}

public async Task<GlobalQuizParticipationStatsResponse> GetGlobalQuizParticipationAsync(
    string? group, CancellationToken ct)
{
    // 1) Rôle STUDENT
    var studentRoleId = await _db.Roles
        .Where(r => r.NormalizedName == "STUDENT")
        .Select(r => r.Id)
        .FirstOrDefaultAsync(ct);

    // 2) Ensemble des étudiants (global ou groupe)
    IQueryable<string> studentIdsQ = from u in _db.Users
                                     join ur in _db.UserRoles on u.Id equals ur.UserId
                                     where ur.RoleId == studentRoleId
                                     select u.Id;

    if (!string.IsNullOrWhiteSpace(group))
    {
        var g = group.Trim().ToLower();
        studentIdsQ = from u in _db.Users
                      join ur in _db.UserRoles on u.Id equals ur.UserId
                      where ur.RoleId == studentRoleId
                         && u.Group != null
                         && u.Group.ToLower() == g
                      select u.Id;
    }

    var totalStudentsCount = await studentIdsQ.Distinct().CountAsync(ct);

    // 3) Participants = étudiants distincts ayant au moins une tentative (tous quiz)
    IQueryable<string> participantsQ =
        from a in _db.Attempts
        join ur in _db.UserRoles on a.UserId equals ur.UserId
        where ur.RoleId == studentRoleId
        select a.UserId;

    if (!string.IsNullOrWhiteSpace(group))
    {
        var g = group.Trim().ToLower();
        participantsQ =
            from a in _db.Attempts
            join u in _db.Users on a.UserId equals u.Id
            join ur in _db.UserRoles on u.Id equals ur.UserId
            where ur.RoleId == studentRoleId
              && u.Group != null
              && u.Group.ToLower() == g
            select a.UserId;
    }

    var participantsCount = await participantsQ.Distinct().CountAsync(ct);

    var rate = totalStudentsCount == 0 ? 0.0
             : Math.Round((participantsCount * 100.0) / totalStudentsCount, 2);

    return new GlobalQuizParticipationStatsResponse
    {
        ParticipantsCount = participantsCount,
        TotalStudentsCount = totalStudentsCount,
        ParticipationRatePercent = rate
    };
}
    
}
