using CarRentalSearch.Api.Controllers;
using CarRentalSearch.Application.DTOs;
using CarRentalSearch.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarRentalSearch.Test.Api;

public class VehiclesControllerTest
{
    private readonly Mock<IVehicleSearchService> _vehicleSearchServiceMock;
    private readonly Mock<ILogger<VehiclesController>> _loggerMock;
    private readonly VehiclesController _sut;

    public VehiclesControllerTest()
    {
        _vehicleSearchServiceMock = new Mock<IVehicleSearchService>();
        _loggerMock = new Mock<ILogger<VehiclesController>>();
        _sut = new VehiclesController(
            _vehicleSearchServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Search_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var expectedResponse = new VehicleSearchResponse(new List<VehicleDto>());

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Search_WithValidRequest_ReturnsCorrectData()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var vehicles = new List<VehicleDto>
        {
            new VehicleDto(
                1,
                "Toyota",
                "Corolla",
                "2023",
                "Sedan",
                "ABC123",
                true,
                new LocationDto(1, "Bogotá Centro", "Calle 26", "Bogota", "Cundinamarca", "Colombia"),
                new MarketDto(1, "Colombia Centro", "Región central")
            )
        };
        var expectedResponse = new VehicleSearchResponse(vehicles);

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VehicleSearchResponse>().Subject;
        response.AvailableVehicles.Should().HaveCount(1);
        response.AvailableVehicles.First().Brand.Should().Be("Toyota");
    }

    [Fact]
    public async Task Search_WhenLocationNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new VehicleSearchRequest("NonExistentCity", "Bogota");

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ThrowsAsync(new KeyNotFoundException("Pickup location 'NonExistentCity' not found"));

        // Act
        var result = await _sut.Search(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Search_WhenLocationNotFound_ReturnsCorrectErrorMessage()
    {
        // Arrange
        var request = new VehicleSearchRequest("NonExistentCity", "Bogota");
        var errorMessage = "Pickup location 'NonExistentCity' not found";

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ThrowsAsync(new KeyNotFoundException(errorMessage));

        // Act
        var result = await _sut.Search(request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task Search_WhenUnexpectedErrorOccurs_ReturnsInternalServerError()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _sut.Search(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task Search_WhenUnexpectedErrorOccurs_ReturnsGenericErrorMessage()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _sut.Search(request);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.Value.Should().Be("An error occurred while searching for vehicles");
    }

    [Fact]
    public async Task Search_WhenUnexpectedErrorOccurs_LogsError()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var exception = new Exception("Database connection error");

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ThrowsAsync(exception);

        // Act
        await _sut.Search(request);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error searching for vehicles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_CallsServiceWithCorrectRequest()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var expectedResponse = new VehicleSearchResponse(new List<VehicleDto>());

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _sut.Search(request);

        // Assert
        _vehicleSearchServiceMock.Verify(
            x => x.SearchVehiclesAsync(It.Is<VehicleSearchRequest>(r =>
                r.PickupLocation == "Bogota" &&
                r.DropoffLocation == "Medellin")),
            Times.Once);
    }

    [Fact]
    public async Task Search_WithEmptyResponse_ReturnsOkWithEmptyList()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var expectedResponse = new VehicleSearchResponse(new List<VehicleDto>());

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VehicleSearchResponse>().Subject;
        response.AvailableVehicles.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithMultipleVehicles_ReturnsAllVehicles()
    {
        // Arrange
        var request = new VehicleSearchRequest("Bogota", "Medellin");
        var vehicles = new List<VehicleDto>
        {
            new VehicleDto(1, "Toyota", "Corolla", "2023", "Sedan", "ABC123", true,
                new LocationDto(1, "Bogotá", "Address1", "Bogota", "Cundinamarca", "Colombia"),
                new MarketDto(1, "Colombia Centro", "Description")),
            new VehicleDto(2, "Chevrolet", "Tracker", "2023", "SUV", "XYZ789", true,
                new LocationDto(1, "Bogotá", "Address1", "Bogota", "Cundinamarca", "Colombia"),
                new MarketDto(1, "Colombia Centro", "Description")),
            new VehicleDto(3, "Renault", "Duster", "2023", "SUV", "DEF456", true,
                new LocationDto(1, "Bogotá", "Address1", "Bogota", "Cundinamarca", "Colombia"),
                new MarketDto(1, "Colombia Centro", "Description"))
        };
        var expectedResponse = new VehicleSearchResponse(vehicles);

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<VehicleSearchResponse>().Subject;
        response.AvailableVehicles.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("Bogota", "Medellin")]
    [InlineData("Cartagena", "Barranquilla")]
    [InlineData("Cali", "Pasto")]
    public async Task Search_WithDifferentCityCombinations_ReturnsOk(string pickup, string dropoff)
    {
        // Arrange
        var request = new VehicleSearchRequest(pickup, dropoff);
        var expectedResponse = new VehicleSearchResponse(new List<VehicleDto>());

        _vehicleSearchServiceMock
            .Setup(x => x.SearchVehiclesAsync(It.IsAny<VehicleSearchRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Search(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}