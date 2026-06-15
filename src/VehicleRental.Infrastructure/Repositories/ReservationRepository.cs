using Microsoft.EntityFrameworkCore;
using VehicleRental.Core.Entities;
using VehicleRental.Core.Interfaces;
using VehicleRental.Infrastructure.Data;

namespace VehicleRental.Infrastructure.Repositories;

public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(AppDbContext context) : base(context) { }

    public override async Task<Reservation?> GetByIdAsync(int id) =>
        await _dbSet.Include(r => r.User).Include(r => r.Vehicle).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId) =>
        await _dbSet.Include(r => r.Vehicle).Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Reservation>> GetByVehicleIdAsync(int vehicleId) =>
        await _dbSet.Include(r => r.User).Where(r => r.VehicleId == vehicleId).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Reservation>> GetByStatusAsync(string status) =>
        await _dbSet.Include(r => r.User).Include(r => r.Vehicle).Where(r => r.Status == status).ToListAsync();

    public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync() =>
        await _dbSet.Include(r => r.User).Include(r => r.Vehicle)
            .Where(r => r.Status == "Active" || r.Status == "Confirmed").ToListAsync();

    public async Task<bool> HasConflictAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var query = _dbSet.Where(r => r.VehicleId == vehicleId
                                   && r.Status != "Cancelled" && r.Status != "Completed"
                                   && r.StartDate < endDate && r.EndDate > startDate);
        if (excludeId.HasValue) query = query.Where(r => r.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Reservation>> GetWithDetailsAsync() =>
        await _dbSet.Include(r => r.User).Include(r => r.Vehicle).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<Reservation?> GetWithDetailsAsync(int id) =>
        await _dbSet.Include(r => r.User).Include(r => r.Vehicle).FirstOrDefaultAsync(r => r.Id == id);
}
