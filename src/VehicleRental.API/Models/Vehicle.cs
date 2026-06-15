using System.ComponentModel.DataAnnotations;

namespace VehicleRental.API.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2100)]
        public int Year { get; set; }

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; } = string.Empty;

        // Economy, SUV, Luxury, Van, Truck
        [Required]
        [MaxLength(30)]
        public string Category { get; set; } = string.Empty;

        [Range(1, 10000)]
        public decimal DailyRate { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? ImageUrl { get; set; }

        public int Mileage { get; set; }

        // Gasoline, Diesel, Electric, Hybrid
        [MaxLength(20)]
        public string FuelType { get; set; } = "Gasoline";

        // Automatic, Manual
        [MaxLength(20)]
        public string Transmission { get; set; } = "Automatic";

        [Range(1, 20)]
        public int Seats { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
