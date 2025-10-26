using System.ComponentModel.DataAnnotations;

namespace CarRentalSearch.Domain.Entities;

public class Vehicle
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public required string Brand { get; set; }
    
    [Required]
    public required string Model { get; set; }
    
    [Required]
    public required string Year { get; set; }
    
    [Required]
    public required string Category { get; set; }
    
    [Required]
    public required string LicensePlate { get; set; }
    
    [Required]
    public bool IsAvailable { get; set; }
    
    public int LocationId { get; set; }
    public Location? CurrentLocation { get; set; }
    
    public int MarketId { get; set; }
    public Market? Market { get; set; }
}