using Microsoft.EntityFrameworkCore;
using VehicleRental.API.Data;
using VehicleRental.API.Models;

namespace VehicleRental.API.Repositories
{
    public interface IReservationRepository
    {
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<Reservation?> GetByIdAsync(int id);
        Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Reservation>> GetByStatusAsync(string status);
        Task<IEnumerable<Reservation>> GetActiveReservationsAsync();
        Task<Reservation> CreateAsync(Reservation reservation);
        Task<Reservation> UpdateAsync(Reservation reservation);
        Task<bool> HasConflictAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null);
    }

    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _context;

        public ReservationRepository(AppDbContext context)
        {
            _context = context;
        }

        // Retrieves all reservations with User and Vehicle navigation properties loaded
        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Retrieves a single reservation with full navigation data; null if not found
        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Retrieves all reservations belonging to a specific customer
        public async Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId)
        {
            return await _context.Reservations
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Retrieves reservations filtered by status (e.g. Pending, Confirmed, Active)
        public async Task<IEnumerable<Reservation>> GetByStatusAsync(string status)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .Where(r => r.Status == status)
                .ToListAsync();
        }

        // Retrieves only Active and Confirmed reservations for dashboard/reporting
        public async Task<IEnumerable<Reservation>> GetActiveReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Vehicle)
                .Where(r => r.Status == "Active" || r.Status == "Confirmed")
                .ToListAsync();
        }

        // Persists a new reservation and returns it with the generated Id
        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        // Updates an existing reservation and saves changes
        public async Task<Reservation> UpdateAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        // Returns true if another non-cancelled/completed reservation overlaps the given period
        public async Task<bool> HasConflictAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var query = _context.Reservations
                .Where(r => r.VehicleId == vehicleId
                         && r.Status != "Cancelled"
                         && r.Status != "Completed"
                         && r.StartDate < endDate
                         && r.EndDate > startDate);

            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);

            return await query.AnyAsync();
        }
    }
}
