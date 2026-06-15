using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using VehicleRental.API.Controllers;
using VehicleRental.API.DTOs;
using VehicleRental.API.Services;
using Xunit;

namespace VehicleRental.Tests.Controllers;

public class AuthControllerTests
{
    private readonly IAuthService _service;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _service = Substitute.For<IAuthService>();
        _controller = new AuthController(_service);
    }

    private static AuthResponseDto MakeAuthResponse(string role = "Customer") => new()
    {
        Token = "jwt.token.here",
        Email = "test@test.com",
        FullName = "John Doe",
        Role = role,
        UserId = 1
    };

    // ─── POST /api/auth/login ───────────────────────────────────────────

    [Fact]
    public async Task Login_HappyPath_Returns200WithTokenWhenCredentialsValid()
    {
        // Arrange
        var dto = new LoginRequestDto { Email = "test@test.com", Password = "Password@123" };
        _service.LoginAsync(dto).Returns(MakeAuthResponse());

        // Act
        var response = await _controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(200, ok.StatusCode);
        var auth = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("Customer", auth.Role);
    }

    [Fact]
    public async Task Login_HappyPath_ReturnsAdminRoleForAdminUser()
    {
        // Arrange
        var dto = new LoginRequestDto { Email = "admin@vehiclerental.com", Password = "Admin@123" };
        _service.LoginAsync(dto).Returns(MakeAuthResponse("Admin"));

        // Act
        var response = await _controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var auth = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.Equal("Admin", auth.Role);
    }

    [Fact]
    public async Task Login_SadPath_Returns401WhenCredentialsInvalid()
    {
        // Arrange
        var dto = new LoginRequestDto { Email = "test@test.com", Password = "WrongPassword" };
        _service.LoginAsync(dto).ReturnsNull();

        // Act
        var response = await _controller.Login(dto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(response.Result);
    }

    // ─── POST /api/auth/register ────────────────────────────────────────

    [Fact]
    public async Task Register_HappyPath_Returns201WithTokenForNewUser()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            FirstName = "John", LastName = "Doe",
            Email = "john@test.com", Password = "Password@123"
        };
        _service.RegisterAsync(dto).Returns(MakeAuthResponse());

        // Act
        var response = await _controller.Register(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Register_SadPath_Returns409WhenEmailAlreadyRegistered()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            FirstName = "John", LastName = "Doe",
            Email = "existing@test.com", Password = "Password@123"
        };
        _service.RegisterAsync(dto).Returns<AuthResponseDto>(_ =>
            throw new InvalidOperationException("An account with this email already exists."));

        // Act
        var response = await _controller.Register(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(response.Result);
    }
}
