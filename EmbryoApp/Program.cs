using EmbryoApp.Data;
using EmbryoApp.Models;
using EmbryoApp.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.StaticFiles;


var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()   // <- gestion des rôles activée
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders(); // <--- important pour reset/confirm



builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
    {
        Title = "Auth Demo",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme()
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") 
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<EmbryoApp.Service.Interface.IModel3DService, EmbryoApp.Service.Implementation.Model3DService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IModelFileService, EmbryoApp.Service.Implementation.ModelFileService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IModelMediaService, EmbryoApp.Service.Implementation.ModelMediaService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IQuizService, EmbryoApp.Service.Implementation.QuizService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IQuestionService, EmbryoApp.Service.Implementation.QuestionService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IAttemptService, EmbryoApp.Service.Implementation.AttemptService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IAttemptAnswerService, EmbryoApp.Service.Implementation.AttemptAnswerService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.INotificationService, EmbryoApp.Service.Implementation.NotificationService>();
builder.Services.AddScoped<EmbryoApp.Service.Interface.IStatisticsService, EmbryoApp.Service.Implementation.StatisticsService>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DataSeeder.SeedRolesAsync(roleManager);
}


// Endpoints natifs Identity → /identity
var identity = app.MapGroup("/identity");
identity.MapIdentityApi<ApplicationUser>();

// Endpoint custom → /auth/register
app.MapCustomAuth();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();   
app.UseAuthorization();


// --- Static files (wwwroot) + mimes pour GLB/GLTF/FBX ---
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRoot))
    Directory.CreateDirectory(webRoot);

// Déclare les types MIME manquants
var contentTypeProvider = new FileExtensionContentTypeProvider();
// 3D
contentTypeProvider.Mappings[".glb"]  = "model/gltf-binary";
contentTypeProvider.Mappings[".gltf"] = "model/gltf+json";
// (FBX n'a pas de MIME officiel standard, on sert du binaire)
contentTypeProvider.Mappings[".fbx"]  = "application/octet-stream";

// Sert tout depuis wwwroot, avec nos mimes custom
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    // Optionnel: Activer si tu veux servir aussi les extensions inconnues
    // ServeUnknownFileTypes = true
});


// app.UseStaticFiles();
app.MapControllers();



var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}