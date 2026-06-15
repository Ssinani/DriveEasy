using Microsoft.EntityFrameworkCore;
using VehicleRental.Core.Entities;
using VehicleRental.Core.Interfaces;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Infrastructure.Repositories;

public class VehicleRepository : BaseRepository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync(DateTime startDate, DateTime endDate)
    {
        var bookedVehicleIds = await _context.Reservations
            .Where(r => r.Status != "Cancelled" && r.Status != "Completed"
                     && r.StartDate < endDate && r.EndDate > startDate)
            .Select(r => r.VehicleId)
            .Distinct()
            .ToListAsync();

        return await _dbSet
            .Where(v => v.IsAvailable && !bookedVehicleIds.Contains(v.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Vehicle>> GetByCategory(string category) =>
        await _dbSet.Where(v => v.Category == category).ToListAsync();

    public async Task<bool> IsAvailableForPeriod(int vehicleId, DateTime startDate, DateTime endDate, int? excludeReservationId = null)
    {
        var vehicle = await _dbSet.FindAsync(vehicleId);
        if (vehicle is null || !vehicle.IsAvailable) return false;

        return !await HasConflictAsync(vehicleId, startDate, endDate, excludeReservationId);
    }

    private async Task<bool> HasConflictAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId)
    {
        var query = _context.Reservations
            .Where(r => r.VehicleId == vehicleId
                     && r.Status != "Cancelled" && r.Status != "Completed"
                     && r.StartDate < endDate && r.EndDate > startDate);

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Vehicle>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType)
    {
        var query = _dbSet.AsQueryable();
        if (!string.IsNullOrEmpty(category)) query = query.Where(v => v.Category == category);
        if (minRate.HasValue) query = query.Where(v => v.DailyRate >= minRate.Value);
        if (maxRate.HasValue) query = query.Where(v => v.DailyRate <= maxRate.Value);
        if (!string.IsNullOrEmpty(fuelType)) query = query.Where(v => v.FuelType == fuelType);
        return await query.ToListAsync();
    }
}
