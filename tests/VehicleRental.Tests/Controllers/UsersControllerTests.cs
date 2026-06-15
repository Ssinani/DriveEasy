using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VehicleRental.API.Controllers;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;
using Xunit;

namespace VehicleRental.Tests.Controllers;

public class UsersControllerTests
{
    private readonly IUserService _service;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _service = Substitute.For<IUserService>();
        _controller = new UsersController(_service);
        SetUser(userId: 1, role: "Admin");
    }

    // Wires a ClaimsPrincipal onto the controller so CurrentUserId resolves correctly
    private void SetUser(int userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static UserReadDto MakeDto(int id = 1, string role = "Customer") => new()
    {
        Id = id,
        FirstName = "Jane",
        LastName = "Doe",
        FullName = "Jane Doe",
        Email = "jane@test.com",
        Role = role,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    // ─── GET /api/users ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_HappyPath_Returns200WithAllUsers()
    {
        // Arrange
        var users = new List<UserReadDto> { MakeDto(1), MakeDto(2) };
        _service.GetAllAsync().Returns(users);

        // Act
        var response = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsAssignableFrom<IEnumerable<UserReadDto>>(ok.Value);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAll_HappyPath_Returns200WithEmptyListWhenNoUsers()
    {
        // Arrange
        _service.GetAllAsync().Returns(new List<UserReadDto>());

        // Act
        var response = await _controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsAssignableFrom<IEnumerable<UserReadDto>>(ok.Value);
        Assert.Empty(result);
    }

    // ─── GET /api/users/{id} ────────────────────────────────────────────

    [Fact]
    public async Task GetById_HappyPath_AdminCanViewAnyUser()
    {
        // Arrange
        SetUser(userId: 1, role: "Admin");
        _service.GetByIdAsync(2).Returns(MakeDto(2));

        // Act
        var response = await _controller.GetById(2);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<UserReadDto>(ok.Value);
        Assert.Equal(2, dto.Id);
    }

    [Fact]
    public async Task GetById_HappyPath_CustomerCanViewOwnProfile()
    {
        // Arrange
        SetUser(userId: 5, role: "Customer");
        _service.GetByIdAsync(5).Returns(MakeDto(5));

        // Act
        var response = await _controller.GetById(5);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_SadPath_CustomerCannotViewOtherUserProfile()
    {
        // Arrange — logged in as user 5, requesting user 2
        SetUser(userId: 5, role: "Customer");

        // Act
        var response = await _controller.GetById(2);

        // Assert
        Assert.IsType<ForbidResult>(response.Result);
    }

    [Fact]
    public async Task GetById_SadPath_Returns404WhenUserNotFound()
    {
        // Arrange
        SetUser(userId: 1, role: "Admin");
        _service.GetByIdAsync(99).ReturnsNull();

        // Act
        var response = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ─── GET /api/users/me ──────────────────────────────────────────────

    [Fact]
    public async Task GetMe_HappyPath_Returns200WithCurrentUserProfile()
    {
        // Arrange
        SetUser(userId: 3, role: "Customer");
        _service.GetByIdAsync(3).Returns(MakeDto(3));

        // Act
        var response = await _controller.GetMe();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<UserReadDto>(ok.Value);
        Assert.Equal(3, dto.Id);
    }

    [Fact]
    public async Task GetMe_SadPath_Returns404WhenProfileNotFound()
    {
        // Arrange
        SetUser(userId: 42, role: "Customer");
        _service.GetByIdAsync(42).ReturnsNull();

        // Act
        var response = await _controller.GetMe();

        // Assert
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ─── PUT /api/users/{id} ────────────────────────────────────────────

    [Fact]
    public async Task Update_HappyPath_Returns200WithUpdatedUser()
    {
        // Arrange
        SetUser(userId: 1, role: "Admin");
        var dto = new UserUpdateDto { FirstName = "Updated", LastName = "Name" };
        _service.UpdateAsync(1, dto).Returns(MakeDto(1));

        // Act
        var response = await _controller.Update(1, dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Update_HappyPath_CustomerCanUpdateOwnProfile()
    {
        // Arrange
        SetUser(userId: 5, role: "Customer");
        var dto = new UserUpdateDto { FirstName = "Self", LastName = "Update" };
        _service.UpdateAsync(5, dto).Returns(MakeDto(5));

        // Act
        var response = await _controller.Update(5, dto);

        // Assert
        Assert.IsType<OkObjectResult>(response.Result);
    }

    [Fact]
    public async Task Update_SadPath_CustomerCannotUpdateOtherUserProfile()
    {
        // Arrange — logged in as 5, trying to update user 2
        SetUser(userId: 5, role: "Customer");
        var dto = new UserUpdateDto { FirstName = "Hacker", LastName = "Attempt" };

        // Act
        var response = await _controller.Update(2, dto);

        // Assert
        Assert.IsType<ForbidResult>(response.Result);
    }

    [Fact]
    public async Task Update_SadPath_Returns404WhenUserNotFound()
    {
        // Arrange
        SetUser(userId: 1, role: "Admin");
        var dto = new UserUpdateDto { FirstName = "X", LastName = "Y" };
        _service.UpdateAsync(99, dto).ReturnsNull();

        // Act
        var response = await _controller.Update(99, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    // ─── PATCH /api/users/{id}/deactivate ───────────────────────────────

    [Fact]
    public async Task Deactivate_HappyPath_Returns204WhenSuccessful()
    {
        // Arrange
        _service.DeactivateAsync(2).Returns(true);

        // Act
        var response = await _controller.Deactivate(2);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task Deactivate_SadPath_Returns404WhenUserNotFound()
    {
        // Arrange
        _service.DeactivateAsync(99).Returns(false);

        // Act
        var response = await _controller.Deactivate(99);

        // Assert
        Assert.IsType<NotFoundObjectResult>(response);
    }

    // ─── PATCH /api/users/{id}/role ─────────────────────────────────────

    [Fact]
    public async Task ChangeRole_HappyPath_Returns204WhenRoleUpdated()
    {
        // Arrange
        _service.ChangeRoleAsync(2, "Admin").Returns(true);

        // Act
        var response = await _controller.ChangeRole(2, new ChangeRoleDto { Role = "Admin" });

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task ChangeRole_SadPath_Returns404WhenUserNotFound()
    {
        // Arrange
        _service.ChangeRoleAsync(99, "Customer").Returns(false);

        // Act
        var response = await _controller.ChangeRole(99, new ChangeRoleDto { Role = "Customer" });

        // Assert
        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task ChangeRole_SadPath_Returns400WhenRoleIsInvalid()
    {
        // Arrange
        _service.ChangeRoleAsync(1, "Superuser")
            .Returns<bool>(_ => throw new ArgumentException("Invalid role 'Superuser'."));

        // Act
        var response = await _controller.ChangeRole(1, new ChangeRoleDto { Role = "Superuser" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }
}
