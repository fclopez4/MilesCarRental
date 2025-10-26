using CarRentalSearch.Api.Application.DTOs;
using CarRentalSearch.Domain.Repositories;

namespace CarRentalSearch.Api.Application.Services;

public interface IVehicleSearchService
{
    Task<VehicleSearchResponse> SearchVehiclesAsync(VehicleSearchRequest request);
}

public class VehicleSearchService : IVehicleSearchService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILogger<VehicleSearchService> _logger;

    public VehicleSearchService(
        IVehicleRepository vehicleRepository,
        ILocationRepository locationRepository,
        ILogger<VehicleSearchService> logger)
    {
        _vehicleRepository = vehicleRepository;
        _locationRepository = locationRepository;
        _logger = logger;
    }

    public async Task<VehicleSearchResponse> SearchVehiclesAsync(VehicleSearchRequest request)
    {
        _logger.LogInformation("Searching for vehicles. Pickup: {PickupLocation}, Dropoff: {DropoffLocation}",
            request.PickupLocation, request.DropoffLocation);

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

        _logger.LogInformation("Found {Count} available vehicles", vehicleDtos.Count);
        return new VehicleSearchResponse(vehicleDtos);
    }
}