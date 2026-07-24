using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using MainBackend.Services;
using MainBackend.Models;
using MainBackend.Configurations;
using MainBackend.Services.BackgroundWorker;
using MainBackend.Services.RagEngine;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Pgvector.EntityFrameworkCore;
using MainBackend.Data.Seeders;
using MainBackend.Services.ChatTitleGeneration;


namespace MainBackend;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);


        // 🔹 Controllers
        // builder.Services.AddControllers();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Encoder =
                System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        // 🔹 Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Config
        builder.Services.Configure<RagEngineOptions>(
            builder.Configuration.GetSection("RagEngine")
        );
        builder.Services.Configure<OpenAIOptions>(
            builder.Configuration.GetSection(OpenAIOptions.SectionName)
        );

        builder.Services.AddSingleton<IEmbeddingService, OpenaiEmbeddingService>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // 🔹 DbContext
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            builder.Configuration.GetConnectionString("DefaultConnection")
        );

        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.UseVector();

        var dataSource = dataSourceBuilder.Build();
        // builder.Services.AddDbContext<AppDbContext>(options =>
        //     options.UseNpgsql(
        //         builder.Configuration.GetConnectionString("DefaultConnection")
        //     )
        // );
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(dataSource, o => o.UseVector());
        });
        builder.Services.AddScoped<IBackgroundJobHandler, GenerateEmbeddingJobHandler>();
        builder.Services.AddScoped<IBackgroundJobHandler, GenerateCuratedQaJobHandler>();
        builder.Services.AddScoped<IBackgroundJobHandler, GenerateExercisesJobHandler>();
        builder.Services.AddHostedService<BackgroundJobWorker>();
        builder.Services.AddSingleton<IChatTitleJobQueue, ChatTitleJobQueue>();
        builder.Services.AddHostedService<ChatTitleGenerationWorker>();

        // 🔹 Services
        builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["access_token"];
                    return Task.CompletedTask;
                }
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-secret")
                ),
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"]
            };
        });
        builder.Services.AddHttpClient();
        builder.Services.AddHttpClient<ExerciseGenerationRagClient>();
        builder.Services.AddHttpClient<IChatTitleGenerator, ChatTitleRagClient>();
        builder.Services.AddScoped<IJwtService, JwtService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<MessageService>();
        builder.Services.AddScoped<ReviewService>();
        builder.Services.AddScoped<IAppDataService, UniDataService>();
        builder.Services.AddScoped<DocumentDataService>();
        builder.Services.AddScoped<IVerifiableQaService, VerifiableQaService>();
        builder.Services.AddScoped<
            ICuratedQaExerciseDashboardService,
            CuratedQaExerciseDashboardService>();

        var app = builder.Build();

        if (args.Contains("--seed-users"))
        {
            using var scope = app.Services.CreateScope();
            await IdentitySeeder.SeedAdminAndLecturerAsync(scope.ServiceProvider);
            return;
        }

        if (args.Contains("--seed-courses"))
        {
            using var scope = app.Services.CreateScope();
            await CourseSeeder.SeedCoursesAsync(scope.ServiceProvider);
            return;
        }

        if (args.Contains("--seed-lecturers"))
        {
            using var scope = app.Services.CreateScope();
            await LecturerSeeder.SeedLecturersAsync(scope.ServiceProvider);
            return;
        }

        if (args.Contains("--seed-documents"))
        {
            using var scope = app.Services.CreateScope();
            await DocumentSeeder.SeedDocumentsAsync(scope.ServiceProvider);
            return;
        }

        if (args.Contains("--seed-majors"))
        {
            using var scope = app.Services.CreateScope();
            await MajorSeeder.SeedMajorsAsync(scope.ServiceProvider);
            return;
        }

        // 🔹 Middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
