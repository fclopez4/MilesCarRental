using CarRentalSearch.Application.DTOs;
using CarRentalSearch.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CarRentalSearch.Application.Services;

public interface IVehicleSearchService
{
    Task<VehicleSearchResponse> SearchVehiclesAsync(VehicleSearchRequest request);
}

public class VehicleSearchService : IVehicleSearchService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<VehicleSearchService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public VehicleSearchService(
        IVehicleRepository vehicleRepository,
        ILocationRepository locationRepository,
        ICacheService cacheService,
        ILogger<VehicleSearchService> logger)
    {
        _vehicleRepository = vehicleRepository;
        _locationRepository = locationRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<VehicleSearchResponse> SearchVehiclesAsync(VehicleSearchRequest request)
    {
        var cacheKey = GenerateCacheKey(request);
        
        // Try to get from cache first
        var cachedResponse = await _cacheService.GetAsync<VehicleSearchResponse>(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Cache hit for vehicle search. Key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogInformation("Cache miss for vehicle search. Key: {CacheKey}", cacheKey);

        // Get locations
        var pickupLocation = await _locationRepository.GetLocationByCityAsync(request.PickupLocation);
        if (pickupLocation == null)
        {
            _logger.LogWarning("Pickup location not found: {PickupLocation}", request.PickupLocation);
            throw new KeyNotFoundException($"Pickup location '{request.PickupLocation}' not found");
        }

        var dropoffLocation = await _locationRepository.GetLocationByCityAsync(request.DropoffLocation);
        if (dropoffLocation == null)
        {
            _logger.LogWarning("Dropoff location not found: {DropoffLocation}", request.DropoffLocation);
            throw new KeyNotFoundException($"Dropoff location '{request.DropoffLocation}' not found");
        }

        var availableVehicles = await _vehicleRepository.GetAvailableVehiclesAsync(pickupLocation.MarketId, pickupLocation.Id);
        var vehicleDtos = availableVehicles.Select(v => new VehicleDto(
            v.Id,
            v.Brand,
            v.Model,
            v.Year,
            v.Category,
            v.LicensePlate,
            v.IsAvailable,
            new LocationDto(
                v.CurrentLocation!.Id,
                v.CurrentLocation.Name,
                v.CurrentLocation.Address,
                v.CurrentLocation.City,
                v.CurrentLocation.State,
                v.CurrentLocation.Country
            ),
            new MarketDto(
                v.Market!.Id,
                v.Market.Name,
                v.Market.Description
            )
        )).ToList();

        var response = new VehicleSearchResponse(vehicleDtos);
        
        // Cache the response
        await _cacheService.SetAsync(cacheKey, response, CacheDuration);

        _logger.LogInformation("Found {Count} available vehicles", vehicleDtos.Count);
        return response;
    }

    private static string GenerateCacheKey(VehicleSearchRequest request)
    {
        return $"vehicle_search:{request.PickupLocation}:{request.DropoffLocation}";
    }

}