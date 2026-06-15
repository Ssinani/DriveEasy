namespace VehicleRental.Core.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Economy, SUV, Luxury, Van, Truck
    public decimal DailyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int Mileage { get; set; }
    public string FuelType { get; set; } = "Gasoline"; // Gasoline, Diesel, Electric, Hybrid
    public string Transmission { get; set; } = "Automatic";
    public int Seats { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
