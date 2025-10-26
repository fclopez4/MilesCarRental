using Microsoft.EntityFrameworkCore;
using CarRentalSearch.Domain.Entities;

namespace CarRentalSearch.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Market> Markets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.CurrentLocation)
            .WithMany(l => l.Vehicles)
            .HasForeignKey(v => v.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Market)
            .WithMany(m => m.Vehicles)
            .HasForeignKey(v => v.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Location>()
            .HasOne(l => l.Market)
            .WithMany(m => m.Locations)
            .HasForeignKey(l => l.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}