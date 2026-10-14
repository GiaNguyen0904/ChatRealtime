using FluentAssertions;
using Messenger.Application.Auth.DTOs;
using Messenger.Application.Auth.Interfaces;
using Messenger.Application.Auth.Services;
using Messenger.Application.Common;
using Messenger.Application.Common.ConflictException.cs;
using Messenger.Domain.Entities;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Messenger.UnitTests.Application.Auth;

public class AuthServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // ============================================================
    // REGISTER
    // ============================================================

    [Fact]
    public async Task RegisterAsync_ShouldReturnAuthResponse_WhenDataIsValid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        var role = new Role
        {
            Id = 1,
            Name = "USER"
        };

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Email = "user@gmail.com",
            Password = "Password123!",
            FullName = "Test User"
        };

        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);
        var accessTokenExpiration = DateTime.UtcNow.AddHours(1);

        passwordHasherMock
            .Setup(x => x.Hash("Password123!"))
            .Returns("hashed_pwd");

        tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("jwt_token");

        tokenServiceMock
            .Setup(x => x.GetAccessTokenExpiration())
            .Returns(accessTokenExpiration);

        tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        tokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("hashed_refresh_token");

        tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiration())
            .Returns(refreshTokenExpiration);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        var result = await sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.ExpiresAt.Should().Be(accessTokenExpiration);

        var userInDb = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "user@gmail.com");

        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Test User");
        userInDb.PasswordHash.Should().Be("hashed_pwd");
        userInDb.RoleId.Should().Be(role.Id);
        userInDb.IsActive.Should().BeTrue();

        var refreshTokenInDb = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userInDb.Id);

        refreshTokenInDb.Should().NotBeNull();
        refreshTokenInDb!.TokenHash.Should().Be("hashed_refresh_token");
        refreshTokenInDb.ExpiresAt.Should().Be(refreshTokenExpiration);
        refreshTokenInDb.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Existing User",
            PasswordHash = "hashed_pwd",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new RegisterRequestDto
        {
            Email = "user@gmail.com",
            Password = "Password123!",
            FullName = "New User"
        };

        // Act
        Func<Task> act = async () =>
            await sut.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("Email is already registered.");
    }

    // ============================================================
    // LOGIN
    // ============================================================

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        var role = new Role
        {
            Id = 1,
            Name = "USER"
        };

        var user = new User
        {
            Id = 1,
            RoleId = role.Id,
            Role = role,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "hashed_pwd",
            IsActive = true
        };

        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "user@gmail.com",
            Password = "Password123!"
        };

        var accessTokenExpiration = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

        passwordHasherMock
            .Setup(x => x.Verify(
                "Password123!",
                "hashed_pwd"))
            .Returns(true);

        tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("jwt_token");

        tokenServiceMock
            .Setup(x => x.GetAccessTokenExpiration())
            .Returns(accessTokenExpiration);

        tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        tokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("hashed_refresh_token");

        tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiration())
            .Returns(refreshTokenExpiration);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.ExpiresAt.Should().Be(accessTokenExpiration);

        passwordHasherMock.Verify(
            x => x.Verify(
                "Password123!",
                "hashed_pwd"),
            Times.Once);

        tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                It.Is<User>(u => u.Email == "user@gmail.com")),
            Times.Once);
        
        var refreshTokenInDb = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id);

        refreshTokenInDb.Should().NotBeNull();
        refreshTokenInDb!.TokenHash.Should().Be("hashed_refresh_token");
        refreshTokenInDb.ExpiresAt.Should().Be(refreshTokenExpiration);
        refreshTokenInDb.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenUserNotFound()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new LoginRequestDto
        {
            Email = "notfound@gmail.com",
            Password = "Password123!"
        };

        // Act
        Func<Task> act = async () =>
            await sut.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "hashed_pwd",
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        passwordHasherMock
            .Setup(x => x.Verify(
                "WrongPassword",
                "hashed_pwd"))
            .Returns(false);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new LoginRequestDto
        {
            Email = "user@gmail.com",
            Password = "WrongPassword"
        };

        // Act
        Func<Task> act = async () =>
            await sut.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenUserIsInactive()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenServiceMock = new Mock<ITokenService>();

        var user = new User
        {
            Id = 1,
            Email = "inactive@gmail.com",
            FullName = "Inactive User",
            PasswordHash = "hashed_pwd",
            IsActive = false
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new LoginRequestDto
        {
            Email = "inactive@gmail.com",
            Password = "Password123!"
        };

        // Act
        Func<Task> act = async () =>
            await sut.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    // ============================================================
    // REFRESH TOKEN
    // ============================================================

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var role = new Role
        {
            Id = 1,
            Name = "USER"
        };

        var user = new User
        {
            Id = 1,
            RoleId = role.Id,
            Role = role,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "hashed_password_dummy",
            IsActive = true
        };

        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);

        var rawToken = "valid_refresh_token";
        var tokenHash = "hashed_refresh_token";

        var oldRefreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        dbContext.RefreshTokens.Add(oldRefreshToken);

        await dbContext.SaveChangesAsync();

        var newRawToken = "new_refresh_token";
        var newTokenHash = "new_hashed_refresh_token";

        var newRefreshTokenExpiration =
            DateTime.UtcNow.AddDays(7);

        var accessTokenExpiration =
            DateTime.UtcNow.AddHours(1);

        tokenServiceMock
            .Setup(x => x.HashRefreshToken(rawToken))
            .Returns(tokenHash);

        tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns(newRawToken);

        tokenServiceMock
            .Setup(x => x.HashRefreshToken(newRawToken))
            .Returns(newTokenHash);

        tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiration())
            .Returns(newRefreshTokenExpiration);

        tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("new_access_token");

        tokenServiceMock
            .Setup(x => x.GetAccessTokenExpiration())
            .Returns(accessTokenExpiration);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new RefreshTokenRequestDto
        {
            RefreshToken = rawToken
        };

        // Act
        var result = await sut.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be(newRawToken);
        result.ExpiresAt.Should().Be(accessTokenExpiration);

        // Token cũ phải bị revoke
        oldRefreshToken.RevokedAt.Should().NotBeNull();

        // Token mới phải được lưu vào DB
        var newTokenInDb = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == newTokenHash);

        newTokenInDb.Should().NotBeNull();
        newTokenInDb!.UserId.Should().Be(user.Id);
        newTokenInDb.ExpiresAt.Should().Be(newRefreshTokenExpiration);
        newTokenInDb.RevokedAt.Should().BeNull();

        // Verify các thao tác quan trọng
        tokenServiceMock.Verify(
            x => x.HashRefreshToken(rawToken),
            Times.Once);

        tokenServiceMock.Verify(
            x => x.GenerateRefreshToken(),
            Times.Once);

        tokenServiceMock.Verify(
            x => x.HashRefreshToken(newRawToken),
            Times.Once);

        tokenServiceMock.Verify(
            x => x.GenerateAccessToken(
                It.Is<User>(u => u.Id == user.Id)),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorized_WhenTokenDoesNotExist()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken("invalid_token"))
            .Returns("invalid_hash");

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new RefreshTokenRequestDto
        {
            RefreshToken = "invalid_token"
        };

        // Act
        Func<Task> act = async () =>
            await sut.RefreshTokenAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has been revoked.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorized_WhenTokenIsExpired()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var rawToken = "expired_token";
        var tokenHash = "hashed_expired_token";

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "hashed_password_dummy",
            IsActive = true
        };

        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken(rawToken))
            .Returns(tokenHash);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        Func<Task> act = async () =>
            await sut.RefreshTokenAsync(
                new RefreshTokenRequestDto
                {
                    RefreshToken = rawToken
                });

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has been revoked.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorized_WhenTokenIsRevoked()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var rawToken = "revoked_token";
        var tokenHash = "hashed_revoked_token";

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "hashed_password_dummy",
            IsActive = true
        };

        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken(rawToken))
            .Returns(tokenHash);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        Func<Task> act = async () =>
            await sut.RefreshTokenAsync(
                new RefreshTokenRequestDto
                {
                    RefreshToken = rawToken
                });

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has been revoked.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorized_WhenUserIsInactive()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var rawToken = "valid_token";
        var tokenHash = "hashed_token";

        var user = new User
        {
            Id = 1,
            Email = "inactive@gmail.com",
            FullName = "Inactive User",
            PasswordHash = "hashed_password_dummy",
            IsActive = false
        };

        var refreshToken = new RefreshToken
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken(rawToken))
            .Returns(tokenHash);

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        Func<Task> act = async () =>
            await sut.RefreshTokenAsync(
                new RefreshTokenRequestDto
                {
                    RefreshToken = rawToken
                });

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("Refresh token has been revoked.");
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeRefreshToken_WhenTokenIsValid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "old_hash",
            IsActive = true
        };

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "hashed_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken("raw_token"))
            .Returns("hashed_token");

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        await sut.LogoutAsync(
            new LogoutRequestDto
            {
                RefreshToken = "raw_token"
            });

        // Assert
        refreshToken.RevokedAt.Should().NotBeNull();
    }
    [Fact]
    public async Task LogoutAsync_ShouldThrowUnauthorized_WhenTokenDoesNotExist()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "old_hash",
            IsActive = true
        };

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            User = user,
            TokenHash = "hashed_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        tokenServiceMock
            .Setup(x => x.HashRefreshToken("invalid_token"))
            .Returns("invalid_hash");

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        // Act
        Func<Task> act = async () =>
            await sut.LogoutAsync(
                new LogoutRequestDto
                {
                    RefreshToken = "invalid_token"
                });

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldChangePasswordAndRevokeRefreshTokens()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var tokenServiceMock = new Mock<ITokenService>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = new User
        {
            Id = 1,
            Email = "user@gmail.com",
            FullName = "Test User",
            PasswordHash = "old_hash",
            IsActive = true
        };

        var refreshToken = new RefreshToken
        {
            UserId = 1,
            User = user,
            TokenHash = "refresh_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync();

        passwordHasherMock
            .Setup(x => x.Verify(
                "OldPassword123!",
                "old_hash"))
            .Returns(true);

        passwordHasherMock
            .Setup(x => x.Hash("NewPassword123!"))
            .Returns("new_hash");

        var sut = new AuthService(
            dbContext,
            tokenServiceMock.Object,
            passwordHasherMock.Object);

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        await sut.ChangePasswordAsync(1, request);

        // Assert
        user.PasswordHash.Should().Be("new_hash");
        refreshToken.RevokedAt.Should().NotBeNull();

        passwordHasherMock.Verify(
            x => x.Verify(
                "OldPassword123!",
                "old_hash"),
            Times.Once);

        passwordHasherMock.Verify(
            x => x.Hash("NewPassword123!"),
            Times.Once);
    }


}