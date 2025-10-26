namespace CarRentalSearch.Api.Application.DTOs;

public record VehicleSearchRequest(
    string PickupLocation,
    string DropoffLocation
);