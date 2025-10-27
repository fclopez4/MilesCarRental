using CarRentalSearch.Application.DTOs;
using CarRentalSearch.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleSearchService _vehicleSearchService;
    private readonly ILogger<VehiclesController> _logger;

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok("Healthy");
    }

    public VehiclesController(
        IVehicleSearchService vehicleSearchService,
        ILogger<VehiclesController> logger)
    {
        _vehicleSearchService = vehicleSearchService;
        _logger = logger;
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(VehicleSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search([FromBody] VehicleSearchRequest request)
    {
        try
        {
            var result = await _vehicleSearchService.SearchVehiclesAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for vehicles");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching for vehicles");
        }
    }
}