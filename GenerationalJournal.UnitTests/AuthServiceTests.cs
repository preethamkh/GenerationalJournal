using GenerationalJournal.Application.Configuration;
using GenerationalJournal.Application.DTOs.Auth;
using GenerationalJournal.Application.Services;
using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using Microsoft.Extensions.Options;
using Moq;

namespace GenerationalJournal.UnitTests;

public class AuthServiceTests
{
    private const string TestKey = "super-secret-testing-key-that-is-at-least-32-characters-long";

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtSettings = new JwtSettings
        {
            Key = TestKey,
            Issuer = "GenerationalJournal",
            Audience = "GenerationalJournal",
            ExpiryMinutes = 60
        };

        _sut = new AuthService(_userRepositoryMock.Object, Options.Create(jwtSettings));
    }

    private static RegisterRequest CreateRegisterRequest(string email = "test@example.com")
    {
        return new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Doe"
        };
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsTokenAndHashesPassword()
    {
        User? createdUser = null;
        _userRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .ReturnsAsync((User u) => u);

        var request = CreateRegisterRequest();

        var result = await _sut.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        Assert.NotNull(createdUser);
        Assert.Equal(request.Email, createdUser!.Email);
        Assert.Equal(request.FirstName, createdUser.FirstName);
        Assert.Equal(request.LastName, createdUser.LastName);
        Assert.True(createdUser.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(createdUser.PasswordHash));
        Assert.NotEqual(request.Password, createdUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(request.Password, createdUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
    {
        _userRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(CreateRegisterRequest()));

        _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUpdatesLastLogin()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Jane",
            LastName = "Doe",
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = "Password123!" };

        var result = await _sut.LoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.Id == user.Id && u.LastLoginAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = "WrongPassword123!" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request));

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsUnauthorizedAccessException()
    {
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest { Email = "missing@example.com", Password = "Password123!" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsActive = false
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var request = new LoginRequest { Email = user.Email, Password = "Password123!" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(request));

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}
