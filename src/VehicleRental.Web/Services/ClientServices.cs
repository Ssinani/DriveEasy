using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace VehicleRental.Web.Services;

// ─── DTOs (mirrored from API) ─────────────────────────────────────────────────
public record AuthResponseDto(string Token, string Email, string FullName, string Role, int UserId);
public record LoginRequestDto(string Email, string Password);
public record RegisterRequestDto(string FirstName, string LastName, string Email, string Password, string? PhoneNumber, string? DriverLicenseNumber);
public record VehicleReadDto(int Id, string Make, string Model, int Year, string LicensePlate, string Category, decimal DailyRate, bool IsAvailable, string? Description, string? ImageUrl, int Mileage, string FuelType, string Transmission, int Seats);
public record VehicleCreateDto(string Make, string Model, int Year, string LicensePlate, string Category, decimal DailyRate, string? Description, string? ImageUrl, int Mileage, string FuelType, string Transmission, int Seats);
public record VehicleUpdateDto(string Make, string Model, int Year, string LicensePlate, string Category, decimal DailyRate, bool IsAvailable, string? Description, string? ImageUrl, int Mileage, string FuelType, string Transmission, int Seats);
public record ReservationReadDto(int Id, int UserId, string CustomerName, string CustomerEmail, int VehicleId, string VehicleName, string LicensePlate, DateTime StartDate, DateTime EndDate, int RentalDays, decimal DailyRate, decimal TotalCost, string Status, string? Notes, DateTime CreatedAt);
public record ReservationCreateDto(int VehicleId, DateTime StartDate, DateTime EndDate, string? Notes);
public record CostEstimateReadDto(int VehicleId, string VehicleName, decimal DailyRate, int Days, decimal Subtotal, decimal DiscountAmount, decimal DiscountPercent, decimal TaxAmount, decimal TotalCost);
public record UserReadDto(int Id, string FirstName, string LastName, string FullName, string Email, string Role, string? PhoneNumber, string? DriverLicenseNumber, bool IsActive, DateTime CreatedAt);

// ─── AUTH SERVICE ─────────────────────────────────────────────────────────────
public interface IAuthClientService
{
    Task<(AuthResponseDto? Result, string? Error)> LoginAsync(LoginRequestDto request);
    Task<(AuthResponseDto? Result, string? Error)> RegisterAsync(RegisterRequestDto request);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<string?> GetRoleAsync();
}

public class AuthClientService : IAuthClientService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly JwtAuthStateProvider _authProvider;

    public AuthClientService(HttpClient http, ILocalStorageService localStorage, JwtAuthStateProvider authProvider)
    {
        _http = http; _localStorage = localStorage; _authProvider = authProvider;
    }

    public async Task<(AuthResponseDto? Result, string? Error)> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
                return (null, "Invalid email or password.");
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (result is not null) await StoreToken(result);
            return (result, null);
        }
        catch
        {
            return (null, "Could not reach the server. Make sure the API is running.");
        }
    }

    public async Task<(AuthResponseDto? Result, string? Error)> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return (null, "An account with this email already exists. Please log in instead.");
            if (!response.IsSuccessStatusCode)
                return (null, "Registration failed. Please try again.");
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return (result, null);
        }
        catch
        {
            return (null, "Could not reach the server. Make sure the API is running.");
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("userRole");
        _authProvider.NotifyUserLoggedOut();
    }

    public Task<string?> GetTokenAsync() => _localStorage.GetItemAsync<string>("authToken").AsTask();
    public Task<string?> GetRoleAsync() => _localStorage.GetItemAsync<string>("userRole").AsTask();

    private async Task StoreToken(AuthResponseDto result)
    {
        await _localStorage.SetItemAsync("authToken", result.Token);
        await _localStorage.SetItemAsync("userRole", result.Role);
        _authProvider.NotifyUserAuthenticated(result.Token);
    }
}

// ─── VEHICLE SERVICE ──────────────────────────────────────────────────────────
public interface IVehicleClientService
{
    Task<List<VehicleReadDto>> GetAllAsync();
    Task<VehicleReadDto?> GetByIdAsync(int id);
    Task<List<VehicleReadDto>> GetAvailableAsync(DateTime startDate, DateTime endDate);
    Task<List<VehicleReadDto>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission);
    Task<VehicleReadDto?> CreateAsync(VehicleCreateDto dto);
    Task<VehicleReadDto?> UpdateAsync(int id, VehicleUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}

public class VehicleClientService : IVehicleClientService
{
    private readonly HttpClient _http;
    public VehicleClientService(HttpClient http) => _http = http;

    public async Task<List<VehicleReadDto>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<VehicleReadDto>>("api/vehicles") ?? new();

    public async Task<VehicleReadDto?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<VehicleReadDto>($"api/vehicles/{id}");

    public async Task<List<VehicleReadDto>> GetAvailableAsync(DateTime s, DateTime e) =>
        await _http.GetFromJsonAsync<List<VehicleReadDto>>(
            $"api/vehicles/available?startDate={s:yyyy-MM-dd}&endDate={e:yyyy-MM-dd}") ?? new();

    public async Task<List<VehicleReadDto>> SearchAsync(string? category, decimal? minRate, decimal? maxRate, string? fuelType, string? transmission)
    {
        var q = $"api/vehicles/search?category={category}&minRate={minRate}&maxRate={maxRate}&fuelType={fuelType}&transmission={transmission}";
        return await _http.GetFromJsonAsync<List<VehicleReadDto>>(q) ?? new();
    }

    public async Task<VehicleReadDto?> CreateAsync(VehicleCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync("api/vehicles", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<VehicleReadDto>() : null;
    }

    public async Task<VehicleReadDto?> UpdateAsync(int id, VehicleUpdateDto dto)
    {
        var r = await _http.PutAsJsonAsync($"api/vehicles/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<VehicleReadDto>() : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var r = await _http.DeleteAsync($"api/vehicles/{id}");
        return r.IsSuccessStatusCode;
    }
}

// ─── RESERVATION SERVICE ──────────────────────────────────────────────────────
public interface IReservationClientService
{
    Task<List<ReservationReadDto>> GetAllAsync();
    Task<List<ReservationReadDto>> GetMyReservationsAsync();
    Task<CostEstimateReadDto?> EstimateCostAsync(int vehicleId, DateTime startDate, DateTime endDate);
    Task<ReservationReadDto?> CreateAsync(ReservationCreateDto dto);
    Task<bool> CancelAsync(int id, string reason);
    Task<bool> ConfirmAsync(int id);
    Task<bool> CompleteAsync(int id);
}

public class ReservationClientService : IReservationClientService
{
    private readonly HttpClient _http;
    public ReservationClientService(HttpClient http) => _http = http;

    public async Task<List<ReservationReadDto>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<ReservationReadDto>>("api/reservations") ?? new();

    public async Task<List<ReservationReadDto>> GetMyReservationsAsync() =>
        await _http.GetFromJsonAsync<List<ReservationReadDto>>("api/reservations/my") ?? new();

    public async Task<CostEstimateReadDto?> EstimateCostAsync(int vehicleId, DateTime s, DateTime e) =>
        await _http.GetFromJsonAsync<CostEstimateReadDto>(
            $"api/reservations/estimate?vehicleId={vehicleId}&startDate={s:yyyy-MM-dd}&endDate={e:yyyy-MM-dd}");

    public async Task<ReservationReadDto?> CreateAsync(ReservationCreateDto dto)
    {
        var r = await _http.PostAsJsonAsync("api/reservations", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ReservationReadDto>() : null;
    }

    public async Task<bool> CancelAsync(int id, string reason)
    {
        var r = await _http.PatchAsJsonAsync($"api/reservations/{id}/cancel", new { Reason = reason });
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> ConfirmAsync(int id) =>
        (await _http.PatchAsync($"api/reservations/{id}/confirm", null)).IsSuccessStatusCode;

    public async Task<bool> CompleteAsync(int id) =>
        (await _http.PatchAsync($"api/reservations/{id}/complete", null)).IsSuccessStatusCode;
}

// ─── USER SERVICE ─────────────────────────────────────────────────────────────
public record UserUpdateRequest(string FirstName, string LastName, string? PhoneNumber, string? DriverLicenseNumber);

public interface IUserClientService
{
    Task<UserReadDto?> GetMeAsync();
    Task<List<UserReadDto>> GetAllAsync();
    Task<UserReadDto?> UpdateAsync(int id, UserUpdateRequest dto);
    Task<bool> ChangeRoleAsync(int id, string role);
    Task<bool> DeactivateAsync(int id);
}

public class UserClientService : IUserClientService
{
    private readonly HttpClient _http;
    public UserClientService(HttpClient http) => _http = http;

    public async Task<UserReadDto?> GetMeAsync() =>
        await _http.GetFromJsonAsync<UserReadDto>("api/users/me");

    public async Task<List<UserReadDto>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<UserReadDto>>("api/users") ?? new();

    public async Task<UserReadDto?> UpdateAsync(int id, UserUpdateRequest dto)
    {
        var r = await _http.PutAsJsonAsync($"api/users/{id}", dto);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<UserReadDto>() : null;
    }

    public async Task<bool> ChangeRoleAsync(int id, string role)
    {
        var r = await _http.PatchAsJsonAsync($"api/users/{id}/role", new { Role = role });
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var r = await _http.PatchAsync($"api/users/{id}/deactivate", null);
        return r.IsSuccessStatusCode;
    }
}
