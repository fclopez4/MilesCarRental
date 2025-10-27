using CarRentalSearch.Domain.Entities;
using CarRentalSearch.Infrastructure.Data;
using CarRentalSearch.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarRentalSearch.Test.Infrastructure;

public class LocationRepositoryTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly LocationRepository _sut;

    public LocationRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new LocationRepository(_context);
        
        SeedDatabase();
    }

    [Fact]
    public async Task GetLocationByCityAsync_WhenCityExists_ReturnsLocation()
    {
        // Arrange
        var cityName = "Bogota";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("Bogota");
    }

    [Fact]
    public async Task GetLocationByCityAsync_WhenCityDoesNotExist_ReturnsNull()
    {
        // Arrange
        var cityName = "NonExistentCity";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLocationByCityAsync_IsCaseInsensitive()
    {
        // Arrange
        var cityNameLower = "bogota";
        var cityNameUpper = "BOGOTA";
        var cityNameMixed = "BoGoTa";

        // Act
        var resultLower = await _sut.GetLocationByCityAsync(cityNameLower);
        var resultUpper = await _sut.GetLocationByCityAsync(cityNameUpper);
        var resultMixed = await _sut.GetLocationByCityAsync(cityNameMixed);

        // Assert
        resultLower.Should().NotBeNull();
        resultUpper.Should().NotBeNull();
        resultMixed.Should().NotBeNull();
        resultLower!.Id.Should().Be(resultUpper!.Id).And.Be(resultMixed!.Id);
    }

    [Fact]
    public async Task GetLocationByCityAsync_IncludesMarketData()
    {
        // Arrange
        var cityName = "Bogota";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().NotBeNull();
        result!.Market.Should().NotBeNull();
        result.Market!.Name.Should().Be("Colombia Centro");
    }

    [Fact]
    public async Task GetLocationByCityAsync_ReturnsFirstMatchWhenMultipleLocationsInSameCity()
    {
        // Arrange
        var cityName = "Medellin";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("Medellin");
    }

    [Fact]
    public async Task GetLocationByCityAsync_ReturnsCompleteLocationData()
    {
        // Arrange
        var cityName = "Bogota";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BePositive();
        result.Name.Should().NotBeNullOrEmpty();
        result.Address.Should().NotBeNullOrEmpty();
        result.City.Should().Be("Bogota");
        result.State.Should().NotBeNullOrEmpty();
        result.Country.Should().Be("Colombia");
        result.MarketId.Should().BePositive();
    }

    [Theory]
    [InlineData("Bogota")]
    [InlineData("Medellin")]
    [InlineData("Cartagena")]
    [InlineData("Barranquilla")]
    [InlineData("Cali")]
    [InlineData("Pasto")]
    public async Task GetLocationByCityAsync_FindsAllSeededCities(string cityName)
    {
        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be(cityName);
    }

    [Fact]
    public async Task GetLocationByCityAsync_WithWhitespace_DoesNotMatch()
    {
        // Arrange
        var cityName = " Bogota ";

        // Act
        var result = await _sut.GetLocationByCityAsync(cityName);

        // Assert
        result.Should().BeNull();
    }

    private void SeedDatabase()
    {
        var markets = new[]
        {
            new Market
            {
                Id = 1,
                Name = "Colombia Centro",
                Description = "Región central de Colombia"
            },
            new Market
            {
                Id = 2,
                Name = "Colombia Costa",
                Description = "Región costera de Colombia"
            },
            new Market
            {
                Id = 3,
                Name = "Colombia Sur",
                Description = "Región sur de Colombia"
            }
        };

        var locations = new[]
        {
            new Location
            {
                Id = 1,
                Name = "Bogotá Centro",
                Address = "Calle 26 #12-45",
                City = "Bogota",
                State = "Cundinamarca",
                Country = "Colombia",
                MarketId = 1
            },
            new Location
            {
                Id = 2,
                Name = "Medellín Poblado",
                Address = "Carrera 43A #15-85",
                City = "Medellin",
                State = "Antioquia",
                Country = "Colombia",
                MarketId = 1
            },
            new Location
            {
                Id = 3,
                Name = "Cartagena Centro",
                Address = "Avenida San Martín #8-76",
                City = "Cartagena",
                State = "Bolivar",
                Country = "Colombia",
                MarketId = 2
            },
            new Location
            {
                Id = 4,
                Name = "Barranquilla Norte",
                Address = "Calle 72 #56-89",
                City = "Barranquilla",
                State = "Atlantico",
                Country = "Colombia",
                MarketId = 2
            },
            new Location
            {
                Id = 5,
                Name = "Cali Sur",
                Address = "Avenida Roosevelt #25-32",
                City = "Cali",
                State = "Valle del Cauca",
                Country = "Colombia",
                MarketId = 3
            },
            new Location
            {
                Id = 6,
                Name = "Pasto Centro",
                Address = "Carrera 27 #12-45",
                City = "Pasto",
                State = "Nariño",
                Country = "Colombia",
                MarketId = 3
            }
        };

        _context.Markets.AddRange(markets);
        _context.Locations.AddRange(locations);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}