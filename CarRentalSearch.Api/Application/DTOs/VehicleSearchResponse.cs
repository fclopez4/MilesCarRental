namespace CarRentalSearch.Api.Application.DTOs;

public record VehicleSearchResponse(
    IEnumerable<VehicleDto> AvailableVehicles
);