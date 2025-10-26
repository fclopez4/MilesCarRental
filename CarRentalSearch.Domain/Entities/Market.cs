using System.ComponentModel.DataAnnotations;

namespace CarRentalSearch.Domain.Entities;

public class Market
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public required string Name { get; set; }
    
    [Required]
    public required string Description { get; set; }
    
    public ICollection<Location>? Locations { get; set; }
    public ICollection<Vehicle>? Vehicles { get; set; }
}