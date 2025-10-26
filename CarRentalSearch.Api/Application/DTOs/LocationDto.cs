namespace CarRentalSearch.Api.Application.DTOs;

public record LocationDto(
    int Id,
    string Name,
    string Address,
    string City,
    string State,
    string Country
);