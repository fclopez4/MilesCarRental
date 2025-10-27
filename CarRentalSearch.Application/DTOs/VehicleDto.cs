namespace CarRentalSearch.Application.DTOs;

public record VehicleDto(
    int Id,
    string Brand,
    string Model,
    string Year,
    string Category,
    string LicensePlate,
    bool IsAvailable,
    LocationDto CurrentLocation,
    MarketDto Market
);