using Microsoft.EntityFrameworkCore;
using VehicleRental.Core.Entities;

namespace VehicleRental.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Vehicle
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Make).IsRequired().HasMaxLength(50);
            e.Property(v => v.Model).IsRequired().HasMaxLength(50);
            e.Property(v => v.LicensePlate).IsRequired().HasMaxLength(20);
            e.HasIndex(v => v.LicensePlate).IsUnique();
            e.Property(v => v.DailyRate).HasColumnType("decimal(10,2)");
            e.Property(v => v.Category).IsRequired().HasMaxLength(30);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(50);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });

        // Reservation
        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.TotalCost).HasColumnType("decimal(10,2)");
            e.Property(r => r.Status).IsRequired().HasMaxLength(20);
            e.HasOne(r => r.User).WithMany(u => u.Reservations).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Vehicle).WithMany(v => v.Reservations).HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seed admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@vehiclerental.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed vehicles
        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022, LicensePlate = "MK-001-AA", Category = "Economy", DailyRate = 35, IsAvailable = true, FuelType = "Gasoline", Transmission = "Automatic", Seats = 5, Mileage = 15000, Description = "Reliable and fuel-efficient sedan.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 2, Make = "BMW", Model = "X5", Year = 2023, LicensePlate = "MK-002-BB", Category = "SUV", DailyRate = 95, IsAvailable = true, FuelType = "Diesel", Transmission = "Automatic", Seats = 7, Mileage = 8000, Description = "Premium SUV with all-wheel drive.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 3, Make = "Mercedes", Model = "E-Class", Year = 2023, LicensePlate = "MK-003-CC", Category = "Luxury", DailyRate = 130, IsAvailable = true, FuelType = "Gasoline", Transmission = "Automatic", Seats = 5, Mileage = 5000, Description = "Elegant luxury sedan.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 4, Make = "Tesla", Model = "Model 3", Year = 2024, LicensePlate = "MK-004-DD", Category = "Economy", DailyRate = 75, IsAvailable = true, FuelType = "Electric", Transmission = "Automatic", Seats = 5, Mileage = 2000, Description = "Zero-emission electric vehicle.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 5, Make = "Ford", Model = "Transit", Year = 2022, LicensePlate = "MK-005-EE", Category = "Van", DailyRate = 65, IsAvailable = true, FuelType = "Diesel", Transmission = "Manual", Seats = 9, Mileage = 25000, Description = "Spacious van for groups or cargo.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
