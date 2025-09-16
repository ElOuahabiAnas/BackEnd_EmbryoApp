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
        // Compte des modèles 3D
        var modelsCount  = await _db.Models3D.CountAsync(ct);

        // Compte des quiz
        var quizzesCount = await _db.Quizzes.CountAsync(ct);

        // Compte des users ayant le rôle "Student"
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

        return new StatsOverviewResponse
        {
            ModelsCount   = modelsCount,
            QuizzesCount  = quizzesCount,
            StudentsCount = studentsCount
        };
    }
}
