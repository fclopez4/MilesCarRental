using CarRentalSearch.Domain.Entities;

namespace CarRentalSearch.Domain.Repositories;

public interface ILocationRepository
{
    Task<Location?> GetLocationByCityAsync(string city);
}