namespace CarRentalSearch.Application.DTOs;

public record VehicleSearchResponse(
    IEnumerable<VehicleDto> AvailableVehicles
);