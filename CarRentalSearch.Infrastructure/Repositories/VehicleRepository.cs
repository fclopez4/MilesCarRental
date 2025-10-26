using CarRentalSearch.Domain.Entities;
using CarRentalSearch.Domain.Repositories;
using CarRentalSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSearch.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync(int marketId, int locationId)
    {
        return await _context.Vehicles
            .Include(v => v.CurrentLocation)
            .Include(v => v.Market)
            .Where(v => v.IsAvailable &&
                       v.MarketId == marketId &&
                       v.LocationId == locationId)
            .ToListAsync();
    }

    public async Task<bool> UpdateVehicleAvailabilityAsync(int vehicleId, bool isAvailable)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        if (vehicle == null)
            return false;

        vehicle.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();
        return true;
    }
}