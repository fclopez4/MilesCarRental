using CarRentalSearch.Domain.Entities;
using CarRentalSearch.Domain.Repositories;
using CarRentalSearch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSearch.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Location?> GetLocationByCityAsync(string city)
    {
        return await _context.Locations
            .Include(l => l.Market)
            .FirstOrDefaultAsync(l => l.City.ToLower() == city.ToLower());
    }
}