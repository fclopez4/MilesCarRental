using CarRentalSearch.Application.DTOs;
using CarRentalSearch.Application.Services;
using CarRentalSearch.Domain.Entities;
using CarRentalSearch.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarRentalSearch.Test.Application;

public class VehicleSearchServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<ILocationRepository> _locationRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<VehicleSearchService>> _loggerMock;
    private readonly VehicleSearchService _sut;

    public VehicleSearchServiceTests()
    {
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _locationRepositoryMock = new Mock<ILocationRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<VehicleSearchService>>();
        
        _sut = new VehicleSearchService(
            _vehicleRepositoryMock.Object,
            _locationRepositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenCacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var cachedResponse = new VehicleSearchResponse(new List<VehicleDto>());
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync(cachedResponse);

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        result.Should().BeSameAs(cachedResponse);
        _locationRepositoryMock.Verify(x => x.GetLocationByCityAsync(It.IsAny<string>()), Times.Never);
        _vehicleRepositoryMock.Verify(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenPickupLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new VehicleSearchRequest("NonExistentCity", "Bogota");
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("NonExistentCity"))
            .ReturnsAsync((Location?)null);

        // Act
        var act = async () => await _sut.SearchVehiclesAsync(request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pickup location 'NonExistentCity' not found");
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenDropoffLocationNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "NonExistentCity");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        
        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("NonExistentCity"))
            .ReturnsAsync((Location?)null);

        // Act
        var act = async () => await _sut.SearchVehiclesAsync(request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Dropoff location 'NonExistentCity' not found");
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenValidRequest_ReturnsAvailableVehicles()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        var dropoffLocation = CreateLocation(2, "Medellin", 1);
        var market = CreateMarket(1, "Colombia Centro");
        var vehicles = new List<Vehicle>
        {
            CreateVehicle(1, "Toyota", "Corolla", pickupLocation, market),
            CreateVehicle(2, "Chevrolet", "Tracker", pickupLocation, market)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(pickupLocation.MarketId, pickupLocation.Id))
            .ReturnsAsync(vehicles);

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AvailableVehicles.Should().HaveCount(2);
        result.AvailableVehicles.First().Brand.Should().Be("Toyota");
        result.AvailableVehicles.Last().Brand.Should().Be("Chevrolet");
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenValidRequest_CachesResponse()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        var dropoffLocation = CreateLocation(2, "Medellin", 1);
        var market = CreateMarket(1, "Colombia Centro");
        var vehicles = new List<Vehicle>
        {
            CreateVehicle(1, "Toyota", "Corolla", pickupLocation, market)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(vehicles);

        // Act
        await _sut.SearchVehiclesAsync(request);

        // Assert
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                "vehicle_search:Bogota:Medellin",
                It.IsAny<VehicleSearchResponse>(),
                It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchVehiclesAsync_WhenNoVehiclesAvailable_ReturnsEmptyList()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        var dropoffLocation = CreateLocation(2, "Medellin", 1);

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Vehicle>());

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        result.AvailableVehicles.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchVehiclesAsync_MapsVehiclePropertiesCorrectly()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        var dropoffLocation = CreateLocation(2, "Medellin", 1);
        var market = CreateMarket(1, "Colombia Centro");
        var vehicle = CreateVehicle(1, "Toyota", "Corolla", pickupLocation, market);
        vehicle.Year = "2023";
        vehicle.Category = "Sedan";
        vehicle.LicensePlate = "ABC123";

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { vehicle });

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        var vehicleDto = result.AvailableVehicles.First();
        vehicleDto.Id.Should().Be(1);
        vehicleDto.Brand.Should().Be("Toyota");
        vehicleDto.Model.Should().Be("Corolla");
        vehicleDto.Year.Should().Be("2023");
        vehicleDto.Category.Should().Be("Sedan");
        vehicleDto.LicensePlate.Should().Be("ABC123");
        vehicleDto.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task SearchVehiclesAsync_MapsLocationPropertiesCorrectly()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        pickupLocation.Address = "Calle 26 #12-45";
        pickupLocation.State = "Cundinamarca";
        pickupLocation.Country = "Colombia";
        
        var dropoffLocation = CreateLocation(2, "Medellin", 1);
        var market = CreateMarket(1, "Colombia Centro");
        var vehicle = CreateVehicle(1, "Toyota", "Corolla", pickupLocation, market);

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { vehicle });

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        var locationDto = result.AvailableVehicles.First().CurrentLocation;
        locationDto.Name.Should().Be("Bogota");
        locationDto.Address.Should().Be("Calle 26 #12-45");
        locationDto.City.Should().Be("Bogota");
        locationDto.State.Should().Be("Cundinamarca");
        locationDto.Country.Should().Be("Colombia");
    }

    [Fact]
    public async Task SearchVehiclesAsync_MapsMarketPropertiesCorrectly()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var pickupLocation = CreateLocation(1, "Bogota", 1);
        var dropoffLocation = CreateLocation(2, "Medellin", 1);
        var market = CreateMarket(1, "Colombia Centro");
        market.Description = "Región central de Colombia";
        var vehicle = CreateVehicle(1, "Toyota", "Corolla", pickupLocation, market);

        _cacheServiceMock
            .Setup(x => x.GetAsync<VehicleSearchResponse>(It.IsAny<string>()))
            .ReturnsAsync((VehicleSearchResponse?)null);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Bogota"))
            .ReturnsAsync(pickupLocation);
        
        _locationRepositoryMock
            .Setup(x => x.GetLocationByCityAsync("Medellin"))
            .ReturnsAsync(dropoffLocation);
        
        _vehicleRepositoryMock
            .Setup(x => x.GetAvailableVehiclesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { vehicle });

        // Act
        var result = await _sut.SearchVehiclesAsync(request);

        // Assert
        var marketDto = result.AvailableVehicles.First().Market;
        marketDto.Id.Should().Be(1);
        marketDto.Name.Should().Be("Colombia Centro");
        marketDto.Description.Should().Be("Región central de Colombia");
    }

    // Helper methods
    private static Location CreateLocation(int id, string city, int marketId)
    {
        return new Location
        {
            Id = id,
            Name = city,
            City = city,
            Address = "Test Address",
            State = "Test State",
            Country = "Colombia",
            MarketId = marketId
        };
    }

    private static Market CreateMarket(int id, string name)
    {
        return new Market
        {
            Id = id,
            Name = name,
            Description = "Test Description"
        };
    }

    private static Vehicle CreateVehicle(int id, string brand, string model, Location location, Market market)
    {
        return new Vehicle
        {
            Id = id,
            Brand = brand,
            Model = model,
            Year = "2023",
            Category = "SUV",
            LicensePlate = $"TEST{id}",
            IsAvailable = true,
            LocationId = location.Id,
            CurrentLocation = location,
            MarketId = market.Id,
            Market = market
        };
    }
}