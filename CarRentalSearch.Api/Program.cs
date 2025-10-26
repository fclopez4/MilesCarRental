using Microsoft.EntityFrameworkCore;
using Serilog;
using CarRentalSearch.Infrastructure.Data;
using CarRentalSearch.Infrastructure.Repositories;
using CarRentalSearch.Api.Application.Services;
using CarRentalSearch.Domain.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure Seq URL (from config or environment). Default to docker-compose service name + port.
var seqUrl = builder.Configuration["Seq:ServerUrl"]
             ?? Environment.GetEnvironmentVariable("SEQ_URL")
             ?? throw new InvalidOperationException("Seq log connection not found.");;

// Setup Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add PostgreSQL database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();

// Register application services
builder.Services.AddScoped<IVehicleSearchService, VehicleSearchService>();

// Add Redis distributed cache
var redisConnection = builder.Configuration.GetConnectionString("Redis") 
    ?? throw new InvalidOperationException("Connection string 'Redis' not found.");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "MilesCarRental_";
});

var app = builder.Build();

try
{
    Log.Information("Starting Miles Car Rental Search API");

    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    // Add a root endpoint
    app.MapGet("/", () => "Miles Car Rental Search API")
        .WithName("Root")
        .WithOpenApi();

    // Add controllers
    app.MapControllers();

    // Seed the database
    await DbSeeder.SeedData(app.Services);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
