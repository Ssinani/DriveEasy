using VehicleRental.Core.DTOs;

namespace VehicleRental.Core.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(int id);
    Task<IEnumerable<VehicleDto>> GetAvailableAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<VehicleDto>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
    Task<VehicleDto?> UpdateAsync(int id, UpdateVehicleRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IReservationService
{
    Task<IEnumerable<ReservationDto>> GetAllAsync();
    Task<ReservationDto?> GetByIdAsync(int id);
    Task<IEnumerable<ReservationDto>> GetByUserIdAsync(int userId);
    Task<ReservationDto> CreateAsync(int userId, CreateReservationRequest request);
    Task<ReservationDto?> UpdateAsync(int id, int userId, UpdateReservationRequest request, bool isAdmin);
    Task<bool> CancelAsync(int id, int userId, string reason, bool isAdmin);
    Task<bool> ConfirmAsync(int id);
    Task<bool> CompleteAsync(int id);
    Task<CostEstimateResponse> EstimateCostAsync(int vehicleId, DateTime startDate, DateTime endDate);
}

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
}

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request);
    Task<bool> DeactivateAsync(int id);
    Task<bool> ChangeRoleAsync(int id, string role);
}
