using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using MainBackend.Services;
using MainBackend.Configurations;
using Npgsql;


namespace MainBackend;

public class Program
{
    public static void Main(string[] args)
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

        // 🔹 DbContext
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            builder.Configuration.GetConnectionString("DefaultConnection")
        );

        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();
        // builder.Services.AddDbContext<AppDbContext>(options =>
        //     options.UseNpgsql(
        //         builder.Configuration.GetConnectionString("DefaultConnection")
        //     )
        // );
        builder.Services.AddDbContext<AppDbContext>(options => {
                options.UseNpgsql(dataSource);
        });

        // 🔹 Services
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<MessageService>();

        var app = builder.Build();

        // 🔹 Middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseMiddleware<ExceptionMiddleware>();
        app.MapControllers();

        app.Run();
    }
}