using VehicleRental.Core.Entities;

namespace VehicleRental.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Vehicle>> GetByCategory(string category);
    Task<bool> IsAvailableForPeriod(int vehicleId, DateTime startDate, DateTime endDate, int? excludeReservationId = null);
    Task<IEnumerable<Vehicle>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType);
}

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Reservation>> GetByVehicleIdAsync(int vehicleId);
    Task<IEnumerable<Reservation>> GetByStatusAsync(string status);
    Task<IEnumerable<Reservation>> GetActiveReservationsAsync();
    Task<bool> HasConflictAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null);
    Task<IEnumerable<Reservation>> GetWithDetailsAsync();
    Task<Reservation?> GetWithDetailsAsync(int id);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetByRoleAsync(string role);
}
