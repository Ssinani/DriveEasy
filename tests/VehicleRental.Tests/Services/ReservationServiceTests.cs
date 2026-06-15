using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VehicleRental.API.DTOs;
using VehicleRental.API.Models;
using VehicleRental.API.Profiles;
using VehicleRental.API.Repositories;
using VehicleRental.API.Services;
using Xunit;

namespace VehicleRental.Tests.Services;

public class ReservationServiceTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;
    private readonly ReservationService _service;

    public ReservationServiceTests()
    {
        _reservationRepository = Substitute.For<IReservationRepository>();
        _vehicleRepository = Substitute.For<IVehicleRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _service = new ReservationService(_reservationRepository, _vehicleRepository, _mapper, cache);
    }

    private static Vehicle MakeVehicle(bool available = true) => new()
    {
        Id = 1, Make = "Toyota", Model = "Corolla", Year = 2022,
        LicensePlate = "MK-001-AA", Category = "Economy", DailyRate = 35m,
        IsAvailable = available, FuelType = "Gasoline", Transmission = "Automatic",
        Seats = 5, Mileage = 0, Reservations = new List<Reservation>()
    };

    private static Reservation MakeReservation(string status = "Pending", int userId = 1) => new()
    {
        Id = 1, UserId = userId, VehicleId = 1,
        StartDate = DateTime.Today.AddDays(1),
        EndDate = DateTime.Today.AddDays(3),
        TotalCost = 82.6m, Status = status,
        User = new User { Id = userId, FirstName = "John", LastName = "Doe", Email = "john@test.com", Role = "Customer", PasswordHash = "" },
        Vehicle = MakeVehicle()
    };

    // ─── EstimateCostAsync ──────────────────────────────────────────────

    [Fact]
    public async Task EstimateCostAsync_HappyPath_ReturnsCorrectBreakdown()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle());
        var start = DateTime.Today.AddDays(1);
        var end = DateTime.Today.AddDays(4); // 3 days

        // Act
        var result = await _service.EstimateCostAsync(1, start, end);

        // Assert
        Assert.Equal(3, result.Days);
        Assert.Equal(35m, result.DailyRate);
        Assert.Equal(105m, result.Subtotal);   // 3 × 35
        Assert.Equal(0m, result.DiscountPercent); // < 7 days, no discount
        Assert.True(result.TotalCost > 105m);  // tax applied
    }

    [Fact]
    public async Task EstimateCostAsync_HappyPath_AppliesWeeklyDiscount()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle());
        var start = DateTime.Today.AddDays(1);
        var end = start.AddDays(7); // exactly 7 days → 5% discount

        // Act
        var result = await _service.EstimateCostAsync(1, start, end);

        // Assert
        Assert.Equal(5m, result.DiscountPercent);
        Assert.True(result.DiscountAmount > 0);
    }

    [Fact]
    public async Task EstimateCostAsync_HappyPath_AppliesMonthlyDiscount()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle());
        var start = DateTime.Today.AddDays(1);
        var end = start.AddDays(30); // 30 days → 15% discount

        // Act
        var result = await _service.EstimateCostAsync(1, start, end);

        // Assert
        Assert.Equal(15m, result.DiscountPercent);
    }

    [Fact]
    public async Task EstimateCostAsync_SadPath_ThrowsWhenVehicleNotFound()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(999).ReturnsNull();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.EstimateCostAsync(999, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
    }

    [Fact]
    public async Task EstimateCostAsync_SadPath_ThrowsWhenDatesInvalid()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EstimateCostAsync(1, DateTime.Today.AddDays(3), DateTime.Today.AddDays(1)));
    }

    // ─── CreateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_HappyPath_CreatesReservationWithCorrectStatus()
    {
        // Arrange
        var vehicle = MakeVehicle();
        var dto = new ReservationCreateDto
        {
            VehicleId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3)
        };
        _vehicleRepository.GetByIdAsync(1).Returns(vehicle);
        _reservationRepository.HasConflictAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(false);
        _reservationRepository.CreateAsync(Arg.Any<Reservation>())
            .Returns(callInfo => { var r = callInfo.Arg<Reservation>(); r.Id = 5; return r; });
        _reservationRepository.GetByIdAsync(5).Returns(MakeReservation());

        // Act
        var result = await _service.CreateAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenStartDateInPast()
    {
        // Arrange
        var dto = new ReservationCreateDto
        {
            VehicleId = 1,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(2)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(1, dto));
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenSameDates()
    {
        // Arrange
        var sameDate = DateTime.Today.AddDays(1);
        var dto = new ReservationCreateDto { VehicleId = 1, StartDate = sameDate, EndDate = sameDate };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(1, dto));
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenExceeds90Days()
    {
        // Arrange
        var dto = new ReservationCreateDto
        {
            VehicleId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(92)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(1, dto));
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenVehicleNotFound()
    {
        // Arrange
        var dto = new ReservationCreateDto
        {
            VehicleId = 999,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3)
        };
        _vehicleRepository.GetByIdAsync(999).ReturnsNull();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(1, dto));
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenVehicleUnavailable()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle(available: false));
        var dto = new ReservationCreateDto
        {
            VehicleId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(1, dto));
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenDateConflictExists()
    {
        // Arrange
        _vehicleRepository.GetByIdAsync(1).Returns(MakeVehicle());
        _reservationRepository.HasConflictAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(true);
        var dto = new ReservationCreateDto
        {
            VehicleId = 1,
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(3)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(1, dto));
    }

    // ─── CancelAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_HappyPath_CancelsPendingReservation()
    {
        // Arrange
        var reservation = MakeReservation("Pending", userId: 1);
        _reservationRepository.GetByIdAsync(1).Returns(reservation);
        _reservationRepository.UpdateAsync(Arg.Any<Reservation>()).Returns(callInfo => callInfo.Arg<Reservation>());

        // Act
        var result = await _service.CancelAsync(1, userId: 1, reason: "Changed plans", isAdmin: false);

        // Assert
        Assert.True(result);
        Assert.Equal("Cancelled", reservation.Status);
        Assert.Equal("Changed plans", reservation.CancellationReason);
    }

    [Fact]
    public async Task CancelAsync_SadPath_ReturnsFalseWhenNotFound()
    {
        // Arrange
        _reservationRepository.GetByIdAsync(999).ReturnsNull();

        // Act
        var result = await _service.CancelAsync(999, 1, "reason", false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CancelAsync_SadPath_ThrowsWhenAlreadyCompleted()
    {
        // Arrange
        _reservationRepository.GetByIdAsync(1).Returns(MakeReservation("Completed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelAsync(1, 1, "reason", true));
    }

    [Fact]
    public async Task CancelAsync_SadPath_ThrowsWhenCustomerCancelsOtherUsersReservation()
    {
        // Arrange
        _reservationRepository.GetByIdAsync(1).Returns(MakeReservation("Pending", userId: 99));

        // Act & Assert — userId 1 trying to cancel userId 99's reservation
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CancelAsync(1, userId: 1, reason: "x", isAdmin: false));
    }

    // ─── ConfirmAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_HappyPath_ConfirmsPendingReservation()
    {
        // Arrange
        var reservation = MakeReservation("Pending");
        _reservationRepository.GetByIdAsync(1).Returns(reservation);
        _reservationRepository.UpdateAsync(Arg.Any<Reservation>()).Returns(callInfo => callInfo.Arg<Reservation>());

        // Act
        var result = await _service.ConfirmAsync(1);

        // Assert
        Assert.True(result);
        Assert.Equal("Confirmed", reservation.Status);
    }

    [Fact]
    public async Task ConfirmAsync_SadPath_ReturnsFalseWhenNotPending()
    {
        // Arrange
        _reservationRepository.GetByIdAsync(1).Returns(MakeReservation("Confirmed"));

        // Act
        var result = await _service.ConfirmAsync(1);

        // Assert
        Assert.False(result);
    }
}
