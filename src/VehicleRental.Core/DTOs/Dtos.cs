namespace VehicleRental.Core.DTOs;

// ─── AUTH ───────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string? DriverLicenseNumber);

public record AuthResponse(
    string Token,
    string Email,
    string FullName,
    string Role,
    int UserId);

// ─── VEHICLE ─────────────────────────────────────────────────────────────────
public record VehicleDto(
    int Id,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string Category,
    decimal DailyRate,
    bool IsAvailable,
    string? Description,
    string? ImageUrl,
    int Mileage,
    string FuelType,
    string Transmission,
    int Seats);

public record CreateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string Category,
    decimal DailyRate,
    string? Description,
    string? ImageUrl,
    int Mileage,
    string FuelType,
    string Transmission,
    int Seats);

public record UpdateVehicleRequest(
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string Category,
    decimal DailyRate,
    bool IsAvailable,
    string? Description,
    string? ImageUrl,
    int Mileage,
    string FuelType,
    string Transmission,
    int Seats);

// ─── RESERVATION ─────────────────────────────────────────────────────────────
public record ReservationDto(
    int Id,
    int UserId,
    string CustomerName,
    string CustomerEmail,
    int VehicleId,
    string VehicleName,
    string LicensePlate,
    DateTime StartDate,
    DateTime EndDate,
    int RentalDays,
    decimal DailyRate,
    decimal TotalCost,
    string Status,
    string? Notes,
    DateTime CreatedAt);

public record CreateReservationRequest(
    int VehicleId,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes);

public record UpdateReservationRequest(
    DateTime StartDate,
    DateTime EndDate,
    string? Notes);

public record CostEstimateResponse(
    int VehicleId,
    string VehicleName,
    decimal DailyRate,
    int Days,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalCost);

// ─── USER ────────────────────────────────────────────────────────────────────
public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PhoneNumber,
    string? DriverLicenseNumber,
    bool IsActive,
    DateTime CreatedAt);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Address,
    string? DriverLicenseNumber);

// ─── COMMON ──────────────────────────────────────────────────────────────────
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record ApiResponse<T>(bool Success, string? Message, T? Data);
