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

public class UserServiceTests
{
    private readonly IUserRepository _repository;
    private readonly IMapper _mapper;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repository = Substitute.For<IUserRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        _service = new UserService(_repository, _mapper);
    }

    private static User MakeUser(int id = 1, string role = "Customer", bool active = true) => new()
    {
        Id = id,
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@test.com",
        PasswordHash = "hash",
        Role = role,
        IsActive = active,
        CreatedAt = DateTime.UtcNow
    };

    // ─── GetAllAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsMappedUsers()
    {
        // Arrange
        var users = new List<User> { MakeUser(1), MakeUser(2) };
        _repository.GetAllAsync().Returns(users);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Jane", result[0].FirstName);
    }

    [Fact]
    public async Task GetAllAsync_HappyPath_ReturnsEmptyListWhenNoUsers()
    {
        // Arrange
        _repository.GetAllAsync().Returns(new List<User>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    // ─── GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_HappyPath_ReturnsMappedUserWhenFound()
    {
        // Arrange
        _repository.GetByIdAsync(1).Returns(MakeUser(1));

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Jane", result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_SadPath_ReturnsNullWhenUserNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(99).ReturnsNull();

        // Act
        var result = await _service.GetByIdAsync(99);

        // Assert
        Assert.Null(result);
    }

    // ─── UpdateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_HappyPath_ReturnsMappedDtoAfterUpdate()
    {
        // Arrange
        var user = MakeUser(1);
        var dto = new UserUpdateDto
        {
            FirstName = "Updated",
            LastName = "Name",
            PhoneNumber = "044123456",
            DriverLicenseNumber = "DL-999"
        };
        var updatedUser = MakeUser(1);
        updatedUser.FirstName = "Updated";
        updatedUser.LastName = "Name";

        _repository.GetByIdAsync(1).Returns(user);
        _repository.UpdateAsync(Arg.Any<User>()).Returns(updatedUser);

        // Act
        var result = await _service.UpdateAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
        await _repository.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task UpdateAsync_SadPath_ReturnsNullWhenUserNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(99).ReturnsNull();

        // Act
        var result = await _service.UpdateAsync(99, new UserUpdateDto
        {
            FirstName = "X", LastName = "Y"
        });

        // Assert
        Assert.Null(result);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    // ─── DeactivateAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_HappyPath_ReturnsTrueWhenSuccessful()
    {
        // Arrange
        _repository.DeactivateAsync(1).Returns(true);

        // Act
        var result = await _service.DeactivateAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeactivateAsync_SadPath_ReturnsFalseWhenUserNotFound()
    {
        // Arrange
        _repository.DeactivateAsync(99).Returns(false);

        // Act
        var result = await _service.DeactivateAsync(99);

        // Assert
        Assert.False(result);
    }

    // ─── ChangeRoleAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRoleAsync_HappyPath_ReturnsTrueAndUpdatesRole()
    {
        // Arrange
        var user = MakeUser(1, "Customer");
        _repository.GetByIdAsync(1).Returns(user);
        _repository.UpdateAsync(Arg.Any<User>()).Returns(user);

        // Act
        var result = await _service.ChangeRoleAsync(1, "Admin");

        // Assert
        Assert.True(result);
        Assert.Equal("Admin", user.Role);
        await _repository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task ChangeRoleAsync_SadPath_ReturnsFalseWhenUserNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(99).ReturnsNull();

        // Act
        var result = await _service.ChangeRoleAsync(99, "Admin");

        // Assert
        Assert.False(result);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ChangeRoleAsync_SadPath_ThrowsArgumentExceptionForInvalidRole()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ChangeRoleAsync(1, "Superuser"));
    }
}
