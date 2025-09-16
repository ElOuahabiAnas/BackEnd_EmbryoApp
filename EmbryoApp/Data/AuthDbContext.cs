using System.Text.Json;
using EmbryoApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmbryoApp.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser>

{
    public AuthDbContext(DbContextOptions options) : base(options) {}
    

    public DbSet<Model3D>    Models3D   => Set<Model3D>();
    public DbSet<ModelMedia> ModelMedia => Set<ModelMedia>();
    public DbSet<ModelFile>  ModelFiles => Set<ModelFile>();
    
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    
    public DbSet<Question> Questions => Set<Question>();
    
    public DbSet<Attempt> Attempts => Set<Attempt>();

    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    public DbSet<Notification> Notifications => Set<Notification>();
    




    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Model3D ----------
        b.Entity<Model3D>(e =>
        {
            e.ToTable("Model3D");
            e.HasKey(x => x.ModelId);

            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // ✅ QUOTER le nom de colonne
            e.HasCheckConstraint("CK_Model3D_Status",
                "\"Status\" IN ('Draft','Active','Closed')");

            e.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.AuthorUserId);
        });

// ---------- ModelMedia ----------
        b.Entity<ModelMedia>(e =>
        {
            e.ToTable("ModelMedia");
            e.HasKey(x => x.MediaId);

            e.Property(x => x.MediaType)
                .HasConversion<string>()
                .HasMaxLength(20);

            // ✅ QUOTER ici aussi (ou LOWER(...))
            e.HasCheckConstraint("CK_ModelMedia_MediaType",
                "\"MediaType\" IN ('Photo','Video')");

            e.Property(x => x.IsPrimary).HasDefaultValue(false).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");

            e.HasOne(x => x.Model)
                .WithMany()
                .HasForeignKey(x => x.ModelId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ModelId);
            e.HasIndex(x => new { x.ModelId, x.IsPrimary });
        });


        // ---------- ModelFile ----------
        b.Entity<ModelFile>(e =>
        {
            e.ToTable("ModelFiles");
            e.HasKey(x => x.FileId);

            e.Property(x => x.IsPrimary)
             .HasDefaultValue(false)
             .IsRequired();

            e.Property(x => x.CreatedAt)
             .HasDefaultValueSql("NOW()");

            e.HasOne(x => x.Model)
             .WithMany()
             .HasForeignKey(x => x.ModelId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ModelId);
            e.HasIndex(x => new { x.ModelId, x.IsPrimary });
        });
        
        b.Entity<Quiz>(e =>
        {
            e.ToTable("Quiz");
            e.HasKey(x => x.QuizId);

            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            e.HasCheckConstraint("CK_Quiz_Status", "\"Status\" IN ('Draft','Active','Closed')");

            // FK optionnelle vers Model3D, on garde le quiz si le modèle est supprimé
            e.HasOne(x => x.Model)
                .WithMany()                   // si tu veux une collection, ajoute List<Quiz> dans Model3D
                .HasForeignKey(x => x.ModelId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.ModelId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.PublishedAt);
        });
        
        b.Entity<Question>(e =>
        {
            e.ToTable("Question");
            e.HasKey(x => x.QuestionId);

            // Enum en string + contrainte
            e.Property(x => x.QuestionType)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.HasCheckConstraint("CK_Question_Type", "\"QuestionType\" IN ('QCM','VraiFaux','Redaction')");

            // Options: List<string> <-> JSON string
            var listToJson = new ValueConverter<List<string>?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
            );
            e.Property(x => x.Options).HasConversion(listToJson);

            e.Property(x => x.Statement).HasMaxLength(2000).IsRequired();

            e.HasOne(x => x.Quiz)
                .WithMany() // (ajoute List<Question> Questions dans Quiz si tu veux la navigation inverse)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.QuizId);
            e.HasIndex(x => x.QuestionType);
        });
        
        b.Entity<Attempt>(e =>
        {
            e.ToTable("Attempt");
            e.HasKey(x => x.AttemptId);

            e.Property(x => x.Score)
                .HasPrecision(5,2); // 0..100.00

            e.Property(x => x.Duration)
                .HasDefaultValue(0);

            e.Property(x => x.AttemptedAt)
                .HasDefaultValueSql("NOW()"); // PostgreSQL (ajuste si autre SGBD)

            // FK -> AspNetUsers (UserId string)
            e.HasOne(x => x.User)
                .WithMany() // si tu veux : add ICollection<Attempt> Attempts dans ApplicationUser
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK -> Quiz
            e.HasOne(x => x.Quiz)
                .WithMany() // idem, tu peux ajouter ICollection<Attempt> dans Quiz
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.QuizId);
            e.HasIndex(x => x.AttemptedAt);
        });
        
        b.Entity<AttemptAnswer>(e =>
        {
            e.ToTable("AttemptAnswer");
            e.HasKey(x => new { x.AttemptId, x.QuestionId });

            e.Property(x => x.IsCorrect).IsRequired();

            e.HasOne(x => x.Attempt)
                .WithMany()
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.AttemptId);
            e.HasIndex(x => x.QuestionId);
        });
        
        
        b.Entity<Notification>(e =>
        {
            e.ToTable("Notification");
            e.HasKey(x => x.NotificationId);

            e.Property(x => x.Title).HasMaxLength(180).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();

            e.Property(x => x.SentAt).HasDefaultValueSql("NOW()"); // PostgreSQL
            e.Property(x => x.IsRead).HasDefaultValue(false);

            e.HasOne(x => x.User)
                .WithMany() // tu peux ajouter ICollection<Notification> dans ApplicationUser si tu veux
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.IsRead);
            e.HasIndex(x => x.SentAt);
        });
        
        
    }
}