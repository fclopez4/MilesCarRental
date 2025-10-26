using System.ComponentModel.DataAnnotations;

namespace CarRentalSearch.Domain.Entities;

public class Location
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public required string Name { get; set; }
    
    [Required]
    public required string Address { get; set; }
    
    [Required]
    public required string City { get; set; }
    
    [Required]
    public required string State { get; set; }
    
    [Required]
    public required string Country { get; set; }
    
    public int MarketId { get; set; }
    public Market? Market { get; set; }
    
    public ICollection<Vehicle>? Vehicles { get; set; }
}