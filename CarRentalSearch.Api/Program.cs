using Microsoft.EntityFrameworkCore;
using Serilog;
using CarRentalSearch.Infrastructure.Data;
using CarRentalSearch.Infrastructure.Repositories;
using CarRentalSearch.Infrastructure.Services;
using CarRentalSearch.Application.Services;
using CarRentalSearch.Domain.Repositories;

namespace CarRentalSearch.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureLogging(builder);
        ConfigureServices(builder);
        
        var app = builder.Build();
        
        try
        {
            await ConfigureApplication(app);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        var seqUrl = builder.Configuration["Seq:ServerUrl"]
                    ?? Environment.GetEnvironmentVariable("SEQ_URL")
                    ?? throw new InvalidOperationException("Seq log connection not found.");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Seq(seqUrl)
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // API and Documentation
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Database
        ConfigureDatabase(builder);

        // Redis Cache
        ConfigureCache(builder);

        // Application Services
        ConfigureApplicationServices(builder);
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void ConfigureCache(WebApplicationBuilder builder)
    {
        var redisConnection = builder.Configuration.GetConnectionString("Redis") 
            ?? throw new InvalidOperationException("Connection string 'Redis' not found.");

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "MilesCarRental_";
        });
    }

    private static void ConfigureApplicationServices(WebApplicationBuilder builder)
    {
        // Repositories
        builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
        builder.Services.AddScoped<ILocationRepository, LocationRepository>();

        // Application Services
        builder.Services.AddScoped<ICacheService, RedisCacheService>();
        builder.Services.AddScoped<IVehicleSearchService, VehicleSearchService>();
    }

    private static async Task ConfigureApplication(WebApplication app)
    {
        Log.Information("Starting Miles Car Rental Search API");

        // API Documentation
        app.UseSwagger();
        app.UseSwaggerUI();

        // Security
        app.UseHttpsRedirection();

        app.MapGet("/", context =>
        {
            context.Response.Redirect("/swagger");
            return Task.CompletedTask;
        });

        app.MapControllers();

        // Database Initialization
        await DbSeeder.SeedData(app.Services);

        await app.RunAsync();
    }
}
