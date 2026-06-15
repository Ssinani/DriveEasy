using Microsoft.EntityFrameworkCore;
using VehicleRental.API.Data;
using VehicleRental.API.Models;
using VehicleRental.API.Repositories;
using Xunit;

namespace VehicleRental.Tests.Repositories;

public class VehicleRepositoryTests
{
    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static Vehicle MakeVehicle(string plate = "MK-001", string category = "Economy",
        string fuel = "Gasoline", decimal rate = 35m, bool available = true) => new()
    {
        Make = "Toyota", Model = "Corolla", Year = 2022, LicensePlate = plate,
        Category = category, DailyRate = rate, IsAvailable = available,
        FuelType = fuel, Transmission = "Automatic", Seats = 5, Mileage = 0
    };

    // ─── GetAllAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsAllVehicles()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllAsync_HappyPath_ReturnsAllVehicles));
        ctx.Vehicles.AddRange(MakeVehicle("MK-A"), MakeVehicle("MK-B"), MakeVehicle("MK-C"));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = (await repo.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsEmptyWhenNoVehicles()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllAsync_HappyPath_ReturnsEmptyWhenNoVehicles));
        var repo = new VehicleRepository(ctx);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    // ─── GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_HappyPath_ReturnsCorrectVehicle()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetByIdAsync_HappyPath_ReturnsCorrectVehicle));
        var vehicle = MakeVehicle("MK-001");
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = await repo.GetByIdAsync(vehicle.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MK-001", result!.LicensePlate);
    }

    [Fact]
    public async Task GetByIdAsync_SadPath_ReturnsNullForNonExistentId()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetByIdAsync_SadPath_ReturnsNullForNonExistentId));
        var repo = new VehicleRepository(ctx);

        // Act
        var result = await repo.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ─── GetAvailableAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableAsync_HappyPath_ExcludesVehiclesWithConflictingReservations()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAvailableAsync_HappyPath_ExcludesVehiclesWithConflictingReservations));
        var v1 = MakeVehicle("MK-A"); var v2 = MakeVehicle("MK-B"); var v3 = MakeVehicle("MK-C");
        ctx.Vehicles.AddRange(v1, v2, v3);
        var user = new User { FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", Role = "Customer" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        ctx.Reservations.Add(new Reservation
        {
            UserId = user.Id, VehicleId = v1.Id,
            StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5),
            TotalCost = 140, Status = "Confirmed"
        });
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = (await repo.GetAvailableAsync(DateTime.Today.AddDays(2), DateTime.Today.AddDays(4))).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, v => v.Id == v1.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_HappyPath_IncludesVehiclesWithCancelledReservations()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAvailableAsync_HappyPath_IncludesVehiclesWithCancelledReservations));
        var vehicle = MakeVehicle("MK-A");
        ctx.Vehicles.Add(vehicle);
        var user = new User { FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "x", Role = "Customer" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        ctx.Reservations.Add(new Reservation
        {
            UserId = user.Id, VehicleId = vehicle.Id,
            StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5),
            TotalCost = 140, Status = "Cancelled" // cancelled — should not block
        });
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = await repo.GetAvailableAsync(DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        // Assert
        Assert.Contains(result, v => v.Id == vehicle.Id);
    }

    // ─── SearchAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_HappyPath_FiltersByCategory()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SearchAsync_HappyPath_FiltersByCategory));
        ctx.Vehicles.AddRange(
            MakeVehicle("A", "Economy"), MakeVehicle("B", "SUV"), MakeVehicle("C", "Economy"));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = (await repo.SearchAsync("Economy", null, null, null, null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal("Economy", v.Category));
    }

    [Fact]
    public async Task SearchAsync_HappyPath_FiltersByMaxRate()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SearchAsync_HappyPath_FiltersByMaxRate));
        ctx.Vehicles.AddRange(
            MakeVehicle("A", rate: 30m), MakeVehicle("B", rate: 60m), MakeVehicle("C", rate: 100m));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = (await repo.SearchAsync(null, null, 65m, null, null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.True(v.DailyRate <= 65m));
    }

    [Fact]
    public async Task SearchAsync_HappyPath_FiltersByFuelType()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SearchAsync_HappyPath_FiltersByFuelType));
        ctx.Vehicles.AddRange(
            MakeVehicle("A", fuel: "Electric"), MakeVehicle("B", fuel: "Gasoline"), MakeVehicle("C", fuel: "Electric"));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = (await repo.SearchAsync(null, null, null, "Electric", null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal("Electric", v.FuelType));
    }

    [Fact]
    public async Task SearchAsync_SadPath_ReturnsEmptyWhenNoMatches()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SearchAsync_SadPath_ReturnsEmptyWhenNoMatches));
        ctx.Vehicles.Add(MakeVehicle("A", "Economy"));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var result = await repo.SearchAsync("Luxury", null, null, null, null);

        // Assert
        Assert.Empty(result);
    }

    // ─── CreateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_HappyPath_PersistsVehicleWithGeneratedId()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CreateAsync_HappyPath_PersistsVehicleWithGeneratedId));
        var repo = new VehicleRepository(ctx);
        var vehicle = MakeVehicle("MK-NEW");

        // Act
        var result = await repo.CreateAsync(vehicle);

        // Assert
        Assert.True(result.Id > 0);
        Assert.NotNull(await ctx.Vehicles.FindAsync(result.Id));
    }

    // ─── DeleteAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_HappyPath_RemovesVehicleFromDatabase()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteAsync_HappyPath_RemovesVehicleFromDatabase));
        var vehicle = MakeVehicle("MK-DEL");
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        await repo.DeleteAsync(vehicle.Id);

        // Assert
        Assert.Null(await ctx.Vehicles.FindAsync(vehicle.Id));
    }

    [Fact]
    public async Task DeleteAsync_SadPath_ThrowsWhenVehicleNotFound()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteAsync_SadPath_ThrowsWhenVehicleNotFound));
        var repo = new VehicleRepository(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repo.DeleteAsync(9999));
    }

    // ─── LicensePlateExistsAsync ────────────────────────────────────────

    [Fact]
    public async Task LicensePlateExistsAsync_HappyPath_ReturnsTrueWhenPlateExists()
    {
        // Arrange
        using var ctx = CreateContext(nameof(LicensePlateExistsAsync_HappyPath_ReturnsTrueWhenPlateExists));
        ctx.Vehicles.Add(MakeVehicle("MK-001-AA"));
        await ctx.SaveChangesAsync();
        var repo = new VehicleRepository(ctx);

        // Act
        var exists = await repo.LicensePlateExistsAsync("MK-001-AA");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task LicensePlateExistsAsync_SadPath_ReturnsFalseWhenPlateNotFound()
    {
        // Arrange
        using var ctx = CreateContext(nameof(LicensePlateExistsAsync_SadPath_ReturnsFalseWhenPlateNotFound));
        var repo = new VehicleRepository(ctx);

        // Act
        var exists = await repo.LicensePlateExistsAsync("XX-999");

        // Assert
        Assert.False(exists);
    }
}
