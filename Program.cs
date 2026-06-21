using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using MainBackend.Services;
using MainBackend.Configurations;

namespace MainBackend;

public class Program
{
    public static void Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        // 🔹 Controllers
        builder.Services.AddControllers();

        // 🔹 Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Config
        builder.Services.Configure<RagEngineOptions>(
            builder.Configuration.GetSection("RagEngine")
        );

        // 🔹 DbContext
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")
            )
        );

        // 🔹 Services
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<ChatService>();

        var app = builder.Build();

        // 🔹 Middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}