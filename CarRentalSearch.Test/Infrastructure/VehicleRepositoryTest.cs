using CarRentalSearch.Domain.Entities;
using CarRentalSearch.Infrastructure.Data;
using CarRentalSearch.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRentalSearch.Test.Infrastructure;

public class VehicleRepositoryTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly VehicleRepository _sut;

    public VehicleRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new VehicleRepository(_context);
        
        SeedDatabase();
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_ReturnsOnlyAvailableVehicles()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.IsAvailable);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_FiltersByMarketId()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().OnlyContain(v => v.MarketId == marketId);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_FiltersByLocationId()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().OnlyContain(v => v.LocationId == locationId);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_IncludesCurrentLocation()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().OnlyContain(v => v.CurrentLocation != null);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_IncludesMarket()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().OnlyContain(v => v.Market != null);
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_WhenNoVehiclesMatch_ReturnsEmptyList()
    {
        // Arrange
        var marketId = 999;
        var locationId = 999;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableVehiclesAsync_DoesNotReturnUnavailableVehicles()
    {
        // Arrange
        var marketId = 1;
        var locationId = 1;

        // Act
        var result = await _sut.GetAvailableVehiclesAsync(marketId, locationId);

        // Assert
        result.Should().NotContain(v => v.LicensePlate == "UNAVAIL");
    }

    [Fact]
    public async Task UpdateVehicleAvailabilityAsync_WhenVehicleExists_UpdatesAvailability()
    {
        // Arrange
        var vehicleId = 1;
        var newAvailability = false;

        // Act
        var result = await _sut.UpdateVehicleAvailabilityAsync(vehicleId, newAvailability);

        // Assert
        result.Should().BeTrue();
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        vehicle!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateVehicleAvailabilityAsync_WhenVehicleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var vehicleId = 999;
        var newAvailability = false;

        // Act
        var result = await _sut.UpdateVehicleAvailabilityAsync(vehicleId, newAvailability);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateVehicleAvailabilityAsync_CanSetToAvailable()
    {
        // Arrange
        var vehicleId = 3;
        var newAvailability = true;

        // Act
        var result = await _sut.UpdateVehicleAvailabilityAsync(vehicleId, newAvailability);

        // Assert
        result.Should().BeTrue();
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        vehicle!.IsAvailable.Should().BeTrue();
    }

    private void SeedDatabase()
    {
        var market1 = new Market
        {
            Id = 1,
            Name = "Colombia Centro",
            Description = "Región central"
        };

        var market2 = new Market
        {
            Id = 2,
            Name = "Colombia Costa",
            Description = "Región costera"
        };

        var location1 = new Location
        {
            Id = 1,
            Name = "Bogotá Centro",
            Address = "Calle 26 #12-45",
            City = "Bogota",
            State = "Cundinamarca",
            Country = "Colombia",
            MarketId = 1
        };

        var location2 = new Location
        {
            Id = 2,
            Name = "Medellín Poblado",
            Address = "Carrera 43A #15-85",
            City = "Medellin",
            State = "Antioquia",
            Country = "Colombia",
            MarketId = 1
        };

        var location3 = new Location
        {
            Id = 3,
            Name = "Cartagena Centro",
            Address = "Avenida San Martín #8-76",
            City = "Cartagena",
            State = "Bolivar",
            Country = "Colombia",
            MarketId = 2
        };

        var vehicles = new[]
        {
            new Vehicle
            {
                Id = 1,
                Brand = "Toyota",
                Model = "Corolla",
                Year = "2023",
                Category = "Sedan",
                LicensePlate = "ABC123",
                IsAvailable = true,
                LocationId = 1,
                MarketId = 1
            },
            new Vehicle
            {
                Id = 2,
                Brand = "Chevrolet",
                Model = "Tracker",
                Year = "2023",
                Category = "SUV",
                LicensePlate = "XYZ789",
                IsAvailable = true,
                LocationId = 1,
                MarketId = 1
            },
            new Vehicle
            {
                Id = 3,
                Brand = "Renault",
                Model = "Duster",
                Year = "2023",
                Category = "SUV",
                LicensePlate = "UNAVAIL",
                IsAvailable = false,
                LocationId = 1,
                MarketId = 1
            },
            new Vehicle
            {
                Id = 4,
                Brand = "Mazda",
                Model = "CX-30",
                Year = "2023",
                Category = "SUV",
                LicensePlate = "MED001",
                IsAvailable = true,
                LocationId = 2,
                MarketId = 1
            },
            new Vehicle
            {
                Id = 5,
                Brand = "Kia",
                Model = "Sportage",
                Year = "2023",
                Category = "SUV",
                LicensePlate = "CTG001",
                IsAvailable = true,
                LocationId = 3,
                MarketId = 2
            }
        };

        _context.Markets.AddRange(market1, market2);
        _context.Locations.AddRange(location1, location2, location3);
        _context.Vehicles.AddRange(vehicles);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}