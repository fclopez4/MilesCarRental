using CarRentalSearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarRentalSearch.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Markets
            if (!await context.Markets.AnyAsync())
            {
                var markets = new List<Market>
                {
                    new() { Name = "Colombia Centro", Description = "Región central de Colombia" },
                    new() { Name = "Colombia Costa", Description = "Región costera de Colombia" },
                    new() { Name = "Colombia Sur", Description = "Región sur de Colombia" }
                };

                await context.Markets.AddRangeAsync(markets);
                await context.SaveChangesAsync();
                logger.LogInformation("Added market data");

                // Locations
                var locations = new List<Location>
                {
                    // Centro
                    new() { 
                        Name = "Bogotá Centro", 
                        Address = "Calle 26 #12-45", 
                        City = "Bogota", 
                        State = "Cundinamarca", 
                        Country = "Colombia", 
                        MarketId = markets[0].Id 
                    },
                    new() { 
                        Name = "Medellín Poblado", 
                        Address = "Carrera 43A #15-85", 
                        City = "Medellin", 
                        State = "Antioquia", 
                        Country = "Colombia", 
                        MarketId = markets[0].Id 
                    },
                    
                    // Costa
                    new() { 
                        Name = "Cartagena Centro", 
                        Address = "Avenida San Martín #8-76", 
                        City = "Cartagena", 
                        State = "Bolivar", 
                        Country = "Colombia", 
                        MarketId = markets[1].Id 
                    },
                    new() { 
                        Name = "Barranquilla Norte", 
                        Address = "Calle 72 #56-89", 
                        City = "Barranquilla", 
                        State = "Atlantico", 
                        Country = "Colombia", 
                        MarketId = markets[1].Id 
                    },
                    
                    // Sur
                    new() { 
                        Name = "Cali Sur", 
                        Address = "Avenida Roosevelt #25-32", 
                        City = "Cali", 
                        State = "Valle del Cauca", 
                        Country = "Colombia", 
                        MarketId = markets[2].Id 
                    },
                    new() { 
                        Name = "Pasto Centro", 
                        Address = "Carrera 27 #12-45", 
                        City = "Pasto", 
                        State = "Nariño", 
                        Country = "Colombia", 
                        MarketId = markets[2].Id 
                    }
                };

                await context.Locations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
                logger.LogInformation("Added location data");

                // Vehicles
                var vehicles = new List<Vehicle>
                {
                    // Bogotá vehicles
                    new() {
                        Brand = "Toyota",
                        Model = "Corolla",
                        Year = "2023",
                        Category = "Sedan",
                        LicensePlate = "ABC123",
                        IsAvailable = true,
                        LocationId = locations[0].Id,
                        MarketId = markets[0].Id
                    },
                    new() {
                        Brand = "Chevrolet",
                        Model = "Tracker",
                        Year = "2023",
                        Category = "SUV",
                        LicensePlate = "XYZ789",
                        IsAvailable = true,
                        LocationId = locations[0].Id,
                        MarketId = markets[0].Id
                    },

                    // Medellín vehicles
                    new() {
                        Brand = "Renault",
                        Model = "Duster",
                        Year = "2023",
                        Category = "SUV",
                        LicensePlate = "DEF456",
                        IsAvailable = true,
                        LocationId = locations[1].Id,
                        MarketId = markets[0].Id
                    },

                    // Cartagena vehicles
                    new() {
                        Brand = "Volkswagen",
                        Model = "T-Cross",
                        Year = "2023",
                        Category = "SUV",
                        LicensePlate = "GHI789",
                        IsAvailable = true,
                        LocationId = locations[2].Id,
                        MarketId = markets[1].Id
                    },

                    // Barranquilla vehicles
                    new() {
                        Brand = "Kia",
                        Model = "Sportage",
                        Year = "2023",
                        Category = "SUV",
                        LicensePlate = "JKL012",
                        IsAvailable = true,
                        LocationId = locations[3].Id,
                        MarketId = markets[1].Id
                    },

                    // Cali vehicles
                    new() {
                        Brand = "Mazda",
                        Model = "CX-30",
                        Year = "2023",
                        Category = "SUV",
                        LicensePlate = "MNO345",
                        IsAvailable = true,
                        LocationId = locations[4].Id,
                        MarketId = markets[2].Id
                    },

                    // Pasto vehicles
                    new() {
                        Brand = "Nissan",
                        Model = "Versa",
                        Year = "2023",
                        Category = "Sedan",
                        LicensePlate = "PQR678",
                        IsAvailable = true,
                        LocationId = locations[5].Id,
                        MarketId = markets[2].Id
                    }
                };

                await context.Vehicles.AddRangeAsync(vehicles);
                await context.SaveChangesAsync();
                logger.LogInformation("Added vehicle data");
            }
            else
            {
                logger.LogInformation("Database already contains data, skipping seeding");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}