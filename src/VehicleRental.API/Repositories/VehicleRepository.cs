using Microsoft.EntityFrameworkCore;
using VehicleRental.API.Data;
using VehicleRental.API.Models;

namespace VehicleRental.API.Repositories
{
    public interface IVehicleRepository
    {
        Task<IEnumerable<Vehicle>> GetAllAsync();
        Task<Vehicle?> GetByIdAsync(int id);
        Task<IEnumerable<Vehicle>> GetAvailableAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Vehicle>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission);
        Task<Vehicle> CreateAsync(Vehicle vehicle);
        Task<Vehicle> UpdateAsync(Vehicle vehicle);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> LicensePlateExistsAsync(string plate, int? excludeId = null);
        Task<bool> IsAvailableForPeriodAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeReservationId = null);
    }

    public class VehicleRepository : IVehicleRepository
    {
        private readonly AppDbContext _context;

        public VehicleRepository(AppDbContext context)
        {
            _context = context;
        }

        // Retrieves all vehicles with their reservations eagerly loaded
        public async Task<IEnumerable<Vehicle>> GetAllAsync()
        {
            return await _context.Vehicles
                .Include(v => v.Reservations)
                .ToListAsync();
        }

        // Retrieves a single vehicle by its primary key; returns null if not found
        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Reservations)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        // Retrieves vehicles not booked (confirmed/active/pending) in the given date range
        public async Task<IEnumerable<Vehicle>> GetAvailableAsync(DateTime startDate, DateTime endDate)
        {
            var bookedIds = await _context.Reservations
                .Where(r => r.Status != "Cancelled" && r.Status != "Completed"
                         && r.StartDate < endDate && r.EndDate > startDate)
                .Select(r => r.VehicleId)
                .Distinct()
                .ToListAsync();

            return await _context.Vehicles
                .Where(v => v.IsAvailable && !bookedIds.Contains(v.Id))
                .ToListAsync();
        }

        // Searches vehicles by optional filters: category, rate range, fuel type, transmission
        public async Task<IEnumerable<Vehicle>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission)
        {
            var query = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(v => v.Category == category);

            if (minRate.HasValue)
                query = query.Where(v => v.DailyRate >= minRate.Value);

            if (maxRate.HasValue)
                query = query.Where(v => v.DailyRate <= maxRate.Value);

            if (!string.IsNullOrEmpty(fuelType))
                query = query.Where(v => v.FuelType == fuelType);

            if (!string.IsNullOrEmpty(transmission))
                query = query.Where(v => v.Transmission == transmission);

            return await query.ToListAsync();
        }

        // Persists a new vehicle and returns it with the generated Id
        public async Task<Vehicle> CreateAsync(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        // Updates an existing vehicle and saves changes
        public async Task<Vehicle> UpdateAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        // Removes a vehicle by Id; throws KeyNotFoundException if not found
        public async Task DeleteAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id)
                ?? throw new KeyNotFoundException($"Vehicle with id {id} was not found.");
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Vehicles.AnyAsync(v => v.Id == id);
        }

        // Checks if a license plate is already in use (optionally excluding a vehicle being updated)
        public async Task<bool> LicensePlateExistsAsync(string plate, int? excludeId = null)
        {
            var query = _context.Vehicles.Where(v => v.LicensePlate == plate.ToUpper());
            if (excludeId.HasValue)
                query = query.Where(v => v.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        // Returns true if the vehicle has no active/confirmed/pending reservations in the date range
        public async Task<bool> IsAvailableForPeriodAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeReservationId = null)
        {
            var query = _context.Reservations
                .Where(r => r.VehicleId == vehicleId
                         && r.Status != "Cancelled" && r.Status != "Completed"
                         && r.StartDate < endDate && r.EndDate > startDate);

            if (excludeReservationId.HasValue)
                query = query.Where(r => r.Id != excludeReservationId.Value);

            return !await query.AnyAsync();
        }
    }
}
