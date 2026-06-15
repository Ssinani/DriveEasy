using AutoMapper;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VehicleRental.API.DTOs;
using VehicleRental.API.Models;
using VehicleRental.API.Profiles;
using VehicleRental.API.Repositories;
using VehicleRental.API.Services;
using Xunit;

namespace VehicleRental.Tests.Services;

public class VehicleServiceTests
{
    private readonly IVehicleRepository _repository;
    private readonly IMapper _mapper;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _repository = Substitute.For<IVehicleRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new VehicleService(_repository, _mapper);
    }

    private static Vehicle MakeVehicle(int id = 1, bool available = true, decimal rate = 35m) => new()
    {
        Id = id, Make = "Toyota", Model = "Corolla", Year = 2022,
        LicensePlate = "MK-001-AA", Category = "Economy", DailyRate = rate,
        IsAvailable = available, FuelType = "Gasoline", Transmission = "Automatic",
        Seats = 5, Mileage = 5000, Reservations = new List<Reservation>()
    };

    // ─── GetAllAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsAllVehiclesMapped()
    {
        // Arrange
        var vehicles = new List<Vehicle> { MakeVehicle(1), MakeVehicle(2) };
        _repository.GetAllAsync().Returns(vehicles);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Toyota", result[0].Make);
    }

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsEmptyListWhenNoVehicles()
    {
        // Arrange
        _repository.GetAllAsync().Returns(new List<Vehicle>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    // ─── GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_HappyPath_ReturnsVehicleWhenFound()
    {
        // Arrange
        _repository.GetByIdAsync(1).Returns(MakeVehicle(1));

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("Toyota", result.Make);
    }

    [Fact]
    public async Task GetByIdAsync_SadPath_ReturnsNullWhenVehicleNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(999).ReturnsNull();

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    // ─── GetAvailableAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_HappyPath_ReturnsAvailableVehicles()
    {
        // Arrange
        var start = DateTime.Today.AddDays(1);
        var end = DateTime.Today.AddDays(3);
        _repository.GetAvailableAsync(start, end).Returns(new List<Vehicle> { MakeVehicle(1) });

        // Act
        var result = (await _service.GetAvailableAsync(start, end)).ToList();

        // Assert
        Assert.Single(result);
        await _repository.Received(1).GetAvailableAsync(start, end);
    }

    [Fact]
    public async Task GetAvailableAsync_SadPath_ThrowsWhenStartDateAfterEndDate()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAvailableAsync(DateTime.Today.AddDays(3), DateTime.Today.AddDays(1)));
    }

    [Fact]
    public async Task GetAvailableAsync_SadPath_ThrowsWhenStartDateEqualsEndDate()
    {
        // Arrange
        var sameDate = DateTime.Today.AddDays(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAvailableAsync(sameDate, sameDate));
    }

    // ─── CreateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_HappyPath_CreatesAndReturnsVehicle()
    {
        // Arrange
        var dto = new VehicleCreateDto
        {
            Make = "Ford", Model = "Focus", Year = 2023, LicensePlate = "MK-003",
            Category = "Economy", DailyRate = 40, FuelType = "Gasoline",
            Transmission = "Manual", Seats = 5, Mileage = 0
        };
        _repository.LicensePlateExistsAsync("MK-003").Returns(false);
        _repository.CreateAsync(Arg.Any<Vehicle>())
            .Returns(callInfo => { var v = callInfo.Arg<Vehicle>(); v.Id = 10; return v; });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ford", result.Make);
        Assert.Equal(10, result.Id);
    }

    [Fact]
    public async Task CreateAsync_SadPath_ThrowsWhenLicensePlateAlreadyExists()
    {
        // Arrange
        var dto = new VehicleCreateDto { LicensePlate = "MK-001-AA", Make = "X", Model = "Y", Year = 2023, Category = "Economy", DailyRate = 50, FuelType = "Gasoline", Transmission = "Automatic", Seats = 5 };
        _repository.LicensePlateExistsAsync("MK-001-AA").Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    // ─── UpdateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_HappyPath_UpdatesAndReturnsVehicle()
    {
        // Arrange
        var vehicle = MakeVehicle(1);
        var dto = new VehicleUpdateDto
        {
            Make = "Toyota", Model = "Camry", Year = 2023, LicensePlate = "MK-001-AA",
            Category = "Economy", DailyRate = 45, IsAvailable = true,
            FuelType = "Gasoline", Transmission = "Automatic", Seats = 5, Mileage = 6000
        };
        _repository.GetByIdAsync(1).Returns(vehicle);
        _repository.LicensePlateExistsAsync("MK-001-AA", excludeId: 1).Returns(false);
        _repository.UpdateAsync(Arg.Any<Vehicle>()).Returns(callInfo => callInfo.Arg<Vehicle>());

        // Act
        var result = await _service.UpdateAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Camry", result!.Model);
        Assert.Equal(45m, result.DailyRate);
    }

    [Fact]
    public async Task UpdateAsync_SadPath_ReturnsNullWhenVehicleNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(999).ReturnsNull();

        // Act
        var result = await _service.UpdateAsync(999, new VehicleUpdateDto
        {
            Make = "X", Model = "Y", Year = 2023, LicensePlate = "XX-000",
            Category = "Economy", DailyRate = 50, FuelType = "Gasoline",
            Transmission = "Automatic", Seats = 5
        });

        // Assert
        Assert.Null(result);
    }

    // ─── DeleteAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_HappyPath_ReturnsTrueAndCallsDelete()
    {
        // Arrange
        _repository.ExistsAsync(1).Returns(true);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        await _repository.Received(1).DeleteAsync(1);
    }

    [Fact]
    public async Task DeleteAsync_SadPath_ReturnsFalseWhenVehicleNotFound()
    {
        // Arrange
        _repository.ExistsAsync(999).Returns(false);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<int>());
    }
}
