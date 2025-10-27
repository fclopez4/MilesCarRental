namespace CarRentalSearch.Application.DTOs;

public record VehicleSearchRequest(
    string PickupLocation,
    string DropoffLocation
);