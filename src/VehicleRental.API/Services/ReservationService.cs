using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using VehicleRental.API.DTOs;
using VehicleRental.API.Models;
using VehicleRental.API.Repositories;

namespace VehicleRental.API.Services
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationReadDto>> GetAllAsync();
        Task<ReservationReadDto?> GetByIdAsync(int id);
        Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(int userId);
        Task<IEnumerable<ReservationReadDto>> GetByStatusAsync(string status);
        Task<CostEstimateReadDto> EstimateCostAsync(int vehicleId, DateTime startDate, DateTime endDate);
        Task<ReservationReadDto> CreateAsync(int userId, ReservationCreateDto dto);
        Task<ReservationReadDto?> UpdateAsync(int id, int userId, ReservationUpdateDto dto, bool isAdmin);
        Task<bool> CancelAsync(int id, int userId, string reason, bool isAdmin);
        Task<bool> ConfirmAsync(int id);
        Task<bool> CompleteAsync(int id);
    }

    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        private const decimal TaxRate = 0.18m;
        private const string CacheKeyPrefix = "reservation_";

        public ReservationService(
            IReservationRepository reservationRepository,
            IVehicleRepository vehicleRepository,
            IMapper mapper,
            IMemoryCache cache)
        {
            _reservationRepository = reservationRepository;
            _vehicleRepository = vehicleRepository;
            _mapper = mapper;
            _cache = cache;
        }

        // Returns all reservations; used by admin dashboard
        public async Task<IEnumerable<ReservationReadDto>> GetAllAsync()
        {
            var reservations = await _reservationRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ReservationReadDto>>(reservations);
        }

        // Returns a single reservation by Id with a 60-second memory cache
        public async Task<ReservationReadDto?> GetByIdAsync(int id)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";

            if (_cache.TryGetValue(cacheKey, out ReservationReadDto? cached))
                return cached;

            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            var dto = _mapper.Map<ReservationReadDto>(reservation);

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
            _cache.Set(cacheKey, dto, options);

            return dto;
        }

        // Returns all reservations for a specific customer, ordered by creation date descending
        public async Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(int userId)
        {
            var reservations = await _reservationRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ReservationReadDto>>(reservations);
        }

        // Returns reservations filtered by status (e.g. Pending, Active, Completed)
        public async Task<IEnumerable<ReservationReadDto>> GetByStatusAsync(string status)
        {
            var reservations = await _reservationRepository.GetByStatusAsync(status);
            return _mapper.Map<IEnumerable<ReservationReadDto>>(reservations);
        }

        // Estimates cost before booking: applies tiered discount and 18% VAT
        // Tiered discounts: 7+ days = 5%, 14+ days = 10%, 30+ days = 15%
        public async Task<CostEstimateReadDto> EstimateCostAsync(int vehicleId, DateTime startDate, DateTime endDate)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId)
                ?? throw new KeyNotFoundException($"Vehicle with id {vehicleId} was not found.");

            var days = (endDate.Date - startDate.Date).Days;
            if (days < 1)
                throw new ArgumentException("End date must be at least one day after start date.");

            var subtotal = vehicle.DailyRate * days;
            var discountPercent = GetDiscountPercent(days);
            var discountAmount = subtotal * discountPercent;
            var afterDiscount = subtotal - discountAmount;
            var taxAmount = afterDiscount * TaxRate;
            var totalCost = afterDiscount + taxAmount;

            return new CostEstimateReadDto
            {
                VehicleId = vehicleId,
                VehicleName = $"{vehicle.Make} {vehicle.Model} ({vehicle.Year})",
                DailyRate = vehicle.DailyRate,
                Days = days,
                Subtotal = Math.Round(subtotal, 2),
                DiscountAmount = Math.Round(discountAmount, 2),
                DiscountPercent = discountPercent * 100,
                TaxAmount = Math.Round(taxAmount, 2),
                TotalCost = Math.Round(totalCost, 2)
            };
        }

        // Creates a reservation after validating all business rules:
        // - Start date cannot be in the past
        // - Minimum 1 day, maximum 90 days
        // - Vehicle must be marked as available
        // - No overlapping reservations for the same vehicle
        public async Task<ReservationReadDto> CreateAsync(int userId, ReservationCreateDto dto)
        {
            if (dto.StartDate.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Reservation start date cannot be in the past.");

            if (dto.StartDate.Date >= dto.EndDate.Date)
                throw new ArgumentException("End date must be at least one day after start date.");

            var days = (dto.EndDate.Date - dto.StartDate.Date).Days;
            if (days > 90)
                throw new ArgumentException("Reservations cannot exceed 90 days.");

            var vehicle = await _vehicleRepository.GetByIdAsync(dto.VehicleId)
                ?? throw new KeyNotFoundException($"Vehicle with id {dto.VehicleId} was not found.");

            if (!vehicle.IsAvailable)
                throw new InvalidOperationException("This vehicle is not currently available for rental.");

            if (await _reservationRepository.HasConflictAsync(dto.VehicleId, dto.StartDate, dto.EndDate))
                throw new InvalidOperationException("Vehicle is already reserved for the selected dates.");

            var estimate = await EstimateCostAsync(dto.VehicleId, dto.StartDate, dto.EndDate);

            var reservation = new Reservation
            {
                UserId = userId,
                VehicleId = dto.VehicleId,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                TotalCost = estimate.TotalCost,
                Status = "Pending",
                Notes = dto.Notes
            };

            var created = await _reservationRepository.CreateAsync(reservation);
            // Reload with navigation properties for correct DTO mapping
            var full = await _reservationRepository.GetByIdAsync(created.Id);
            return _mapper.Map<ReservationReadDto>(full!);
        }

        // Updates dates/notes on a Pending reservation; only owner or admin may update
        public async Task<ReservationReadDto?> UpdateAsync(int id, int userId, ReservationUpdateDto dto, bool isAdmin)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            if (!isAdmin && reservation.UserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to modify this reservation.");

            if (reservation.Status != "Pending")
                throw new InvalidOperationException("Only pending reservations can be modified.");

            var days = (dto.EndDate.Date - dto.StartDate.Date).Days;
            if (days < 1)
                throw new ArgumentException("End date must be at least one day after start date.");

            if (await _reservationRepository.HasConflictAsync(reservation.VehicleId, dto.StartDate, dto.EndDate, excludeId: id))
                throw new InvalidOperationException("Vehicle is already reserved for the selected dates.");

            reservation.StartDate = dto.StartDate.Date;
            reservation.EndDate = dto.EndDate.Date;
            reservation.Notes = dto.Notes;
            reservation.UpdatedAt = DateTime.UtcNow;

            var estimate = await EstimateCostAsync(reservation.VehicleId, dto.StartDate, dto.EndDate);
            reservation.TotalCost = estimate.TotalCost;

            var updated = await _reservationRepository.UpdateAsync(reservation);

            // Invalidate cached entry so the updated data is served on next GetById
            _cache.Remove($"{CacheKeyPrefix}{id}");

            return _mapper.Map<ReservationReadDto>(updated);
        }

        // Cancels a reservation; only owner or admin may cancel; cannot cancel completed reservations
        public async Task<bool> CancelAsync(int id, int userId, string reason, bool isAdmin)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return false;

            if (!isAdmin && reservation.UserId != userId)
                throw new UnauthorizedAccessException("You are not authorized to cancel this reservation.");

            if (reservation.Status == "Completed" || reservation.Status == "Cancelled")
                throw new InvalidOperationException("Cannot cancel a reservation that is already completed or cancelled.");

            reservation.Status = "Cancelled";
            reservation.CancellationReason = reason;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _reservationRepository.UpdateAsync(reservation);
            _cache.Remove($"{CacheKeyPrefix}{id}");
            return true;
        }

        // Confirms a Pending reservation (admin action)
        public async Task<bool> ConfirmAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null || reservation.Status != "Pending") return false;

            reservation.Status = "Confirmed";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);
            _cache.Remove($"{CacheKeyPrefix}{id}");
            return true;
        }

        // Marks an Active or Confirmed reservation as Completed (admin action)
        public async Task<bool> CompleteAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null || (reservation.Status != "Active" && reservation.Status != "Confirmed"))
                return false;

            reservation.Status = "Completed";
            reservation.UpdatedAt = DateTime.UtcNow;
            await _reservationRepository.UpdateAsync(reservation);
            _cache.Remove($"{CacheKeyPrefix}{id}");
            return true;
        }

        // Tiered discount: 7+ days = 5%, 14+ days = 10%, 30+ days = 15%
        private static decimal GetDiscountPercent(int days) => days switch
        {
            >= 30 => 0.15m,
            >= 14 => 0.10m,
            >= 7  => 0.05m,
            _     => 0m
        };
    }
}
