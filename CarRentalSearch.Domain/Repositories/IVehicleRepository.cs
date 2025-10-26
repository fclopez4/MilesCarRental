using CarRentalSearch.Domain.Entities;

namespace CarRentalSearch.Domain.Repositories;

public interface IVehicleRepository
{
    Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync(int marketId, int locationId);
    Task<bool> UpdateVehicleAvailabilityAsync(int vehicleId, bool isAvailable);
}