using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VehicleRental.API.Controllers;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;
using Xunit;

namespace VehicleRental.Tests.Controllers;

public class VehiclesControllerTests
{
    private readonly IVehicleService _service;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _service = Substitute.For<IVehicleService>();
        _controller = new VehiclesController(_service);
    }

    private static VehicleReadDto MakeDto(int id = 1, bool available = true) => new()
    {
        Id = id, Make = "Toyota", Model = "Corolla", Year = 2022,
        LicensePlate = "MK-001-AA", Category = "Economy", DailyRate = 35m,
        IsAvailable = available, FuelType = "Gasoline", Transmission = "Automatic", Seats = 5
    };

    // ─── GET /api/vehicles ──────────────────────────────────────────────

    [Fact]
    public async Task GetAll_HappyPath_Returns200WithVehicleList()
    {
        // Arrange
        _service.GetAllAsync().Returns(new List<VehicleReadDto> { MakeDto(1), MakeDto(2) });

        // Act
        var response = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(200, ok.StatusCode);
        var list = Assert.IsAssignableFrom<IEnumerable<VehicleReadDto>>(ok.Value);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task GetAll_HappyPath_ReturnsEmptyListWhenNoVehicles()
    {
        // Arrange
        _service.GetAllAsync().Returns(new List<VehicleReadDto>());

        // Act
        var response = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Empty((IEnumerable<VehicleReadDto>)ok.Value!);
    }

    // ─── GET /api/vehicles/{id} ─────────────────────────────────────────

    [Fact]
    public async Task GetById_HappyPath_Returns200WithVehicle()
    {
        // Arrange
        _service.GetByIdAsync(1).Returns(MakeDto(1));

        // Act
        var response = await _controller.GetById(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<VehicleReadDto>(ok.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetById_SadPath_Returns404WhenVehicleNotFound()
    {
        // Arrange
        _service.GetByIdAsync(999).ReturnsNull();

        // Act
        var response = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ─── GET /api/vehicles/available ───────────────────────────────────

    [Fact]
    public async Task GetAvailable_HappyPath_Returns200WithAvailableVehicles()
    {
        // Arrange
        var start = DateTime.Today.AddDays(1);
        var end = DateTime.Today.AddDays(3);
        _service.GetAvailableAsync(start, end).Returns(new List<VehicleReadDto> { MakeDto(1) });

        // Act
        var response = await _controller.GetAvailable(start, end);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Single((IEnumerable<VehicleReadDto>)ok.Value!);
    }

    [Fact]
    public async Task GetAvailable_SadPath_Returns400WhenDatesInvalid()
    {
        // Arrange
        var start = DateTime.Today.AddDays(5);
        var end = DateTime.Today.AddDays(1);
        _service.GetAvailableAsync(start, end)
            .Returns<IEnumerable<VehicleReadDto>>(_ => throw new ArgumentException("End date must be after start date."));

        // Act
        var response = await _controller.GetAvailable(start, end);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    // ─── POST /api/vehicles ─────────────────────────────────────────────

    [Fact]
    public async Task Create_HappyPath_Returns201WithCreatedVehicle()
    {
        // Arrange
        var dto = new VehicleCreateDto
        {
            Make = "Ford", Model = "Focus", Year = 2023, LicensePlate = "MK-003",
            Category = "Economy", DailyRate = 40, FuelType = "Gasoline",
            Transmission = "Manual", Seats = 5
        };
        _service.CreateAsync(dto).Returns(MakeDto(10));

        // Act
        var response = await _controller.Create(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Create_SadPath_Returns409WhenLicensePlateExists()
    {
        // Arrange
        var dto = new VehicleCreateDto
        {
            Make = "X", Model = "Y", Year = 2023, LicensePlate = "MK-001-AA",
            Category = "Economy", DailyRate = 50, FuelType = "Gasoline",
            Transmission = "Automatic", Seats = 5
        };
        _service.CreateAsync(dto)
            .Returns<VehicleReadDto>(_ => throw new InvalidOperationException("License plate already registered."));

        // Act
        var response = await _controller.Create(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    // ─── PUT /api/vehicles/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Update_HappyPath_Returns200WithUpdatedVehicle()
    {
        // Arrange
        var dto = new VehicleUpdateDto
        {
            Make = "Toyota", Model = "Camry", Year = 2023, LicensePlate = "MK-001-AA",
            Category = "Economy", DailyRate = 45, IsAvailable = true,
            FuelType = "Gasoline", Transmission = "Automatic", Seats = 5
        };
        _service.UpdateAsync(1, dto).Returns(MakeDto(1));

        // Act
        var response = await _controller.Update(1, dto);

        // Assert
        Assert.IsType<OkObjectResult>(response.Result);
    }

    [Fact]
    public async Task Update_SadPath_Returns404WhenVehicleNotFound()
    {
        // Arrange
        var dto = new VehicleUpdateDto
        {
            Make = "X", Model = "Y", Year = 2023, LicensePlate = "XX-000",
            Category = "Economy", DailyRate = 50, FuelType = "Gasoline",
            Transmission = "Automatic", Seats = 5
        };
        _service.UpdateAsync(999, dto).ReturnsNull();

        // Act
        var response = await _controller.Update(999, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ─── DELETE /api/vehicles/{id} ──────────────────────────────────────

    [Fact]
    public async Task Delete_HappyPath_Returns204WhenVehicleDeleted()
    {
        // Arrange
        _service.DeleteAsync(1).Returns(true);

        // Act
        var response = await _controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task Delete_SadPath_Returns404WhenVehicleNotFound()
    {
        // Arrange
        _service.DeleteAsync(999).Returns(false);

        // Act
        var response = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response);
    }
}
