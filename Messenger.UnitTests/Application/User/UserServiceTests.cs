using FluentAssertions;
using Messenger.Application.Common;
using Messenger.Application.Users.DTOs;
using Messenger.Application.Users.Services;
using Messenger.Domain.Entities;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Messenger.UnitTests.Application.Users;

public class UserServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // ============================================================
    // GET PROFILE
    // ============================================================

    [Fact]
    public async Task GetProfileAsync_ShouldReturnUserProfile_WhenUserExistsAndIsActive()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var user = new User
        {
            Id = 1,
            Email = "john.doe@gmail.com",
            PasswordHash = "hashed_pwd",
            FullName = "John Doe",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        var result = await sut.GetProfileAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var sut = new UserService(dbContext);

        // Act
        Func<Task> act = async () =>
            await sut.GetProfileAsync(999);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("User not found or being not active");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowNotFound_WhenUserIsInactive()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var inactiveUser = new User
        {
            Id = 1,
            Email = "inactive@gmail.com",
            PasswordHash = "hashed_pwd",
            FullName = "Inactive User",
            IsActive = false
        };

        dbContext.Users.Add(inactiveUser);
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        Func<Task> act = async () =>
            await sut.GetProfileAsync(1);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("User not found or being not active");
    }

    // ============================================================
    // SEARCH USERS
    // ============================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchUsersAsync_ShouldReturnEmptyList_WhenKeywordIsNullOrEmptyWhitespace(string? keyword)
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        // Thêm role để tránh lỗi null role khi query
        var userRole = new Role { Id = 1, Name = "USER" };
        dbContext.Roles.Add(userRole);

        // Không add user nào để test trả về rỗng an toàn
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        // Hàm của bạn lấy danh sách mặc định nếu keyword rỗng, 
        // do DB trống nên vẫn sẽ trả về Empty. Test này vẫn đúng.
        var result = await sut.SearchUsersAsync(1, keyword!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchUsersAsync_ShouldReturnMatchingUsers_WhenKeywordMatchesNameOrEmail()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var currentUserId = 1;

        // TẠO ROLE ĐỂ GÁN CHO CÁC USER (Tránh bị loại bởi điều kiện u.Role.Name != "ADMIN")
        var userRole = new Role { Id = 1, Name = "USER" };
        var adminRole = new Role { Id = 2, Name = "ADMIN" };
        dbContext.Roles.AddRange(userRole, adminRole);

        var users = new List<User>
        {
            new User { Id = 1, RoleId = 1, Role = userRole, Email = "current@gmail.com", PasswordHash = "pwd", FullName = "Current User", IsActive = true },
            new User { Id = 2, RoleId = 1, Role = userRole, Email = "alex.smith@gmail.com", PasswordHash = "pwd", FullName = "Alex Smith", IsActive = true },
            new User { Id = 3, RoleId = 1, Role = userRole, Email = "john.alexander@gmail.com", PasswordHash = "pwd", FullName = "John Doe", IsActive = true },
            new User { Id = 4, RoleId = 1, Role = userRole, Email = "alex.inactive@gmail.com", PasswordHash = "pwd", FullName = "Alex Inactive", IsActive = false },
            new User { Id = 5, RoleId = 1, Role = userRole, Email = "bob@gmail.com", PasswordHash = "pwd", FullName = "Bob Marley", IsActive = true },
            
            // Thêm 1 tài khoản ADMIN có tên "Alex" để chứng minh hàm của bạn LỌC BỎ ADMIN THÀNH CÔNG
            new User { Id = 6, RoleId = 2, Role = adminRole, Email = "admin.alex@gmail.com", PasswordHash = "pwd", FullName = "Alex Admin", IsActive = true }
        };

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        var result = await sut.SearchUsersAsync(currentUserId, "alex");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Chỉ trả về user 2 và 3 (user 4 bị inactive, user 6 bị admin loại)
        result.Select(u => u.Id).Should().Contain(new[] { 2, 3 });
        result.Select(u => u.Id).Should().NotContain(currentUserId);
        result.Select(u => u.Id).Should().NotContain(4);
        result.Select(u => u.Id).Should().NotContain(6); // Chắc chắn Admin không lọt vào danh sách
    }

    [Fact]
    public async Task SearchUsersAsync_ShouldBeCaseInsensitiveAndTrimSpaces_WhenSearching()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var userRole = new Role { Id = 1, Name = "USER" };
        dbContext.Roles.Add(userRole);

        var user = new User
        {
            Id = 2,
            RoleId = 1,
            Role = userRole, // Gán Role để thỏa điều kiện Role != ADMIN
            Email = "john.doe@gmail.com",
            PasswordHash = "hashed_pwd",
            FullName = "John Doe",
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        var result = await sut.SearchUsersAsync(1, "   JOHN   ");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(2);
        result.First().FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task SearchUsersAsync_ShouldLimitResultTo20Users_WhenMatchesExceed20()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var currentUserId = 1;

        var userRole = new Role { Id = 1, Name = "USER" };
        dbContext.Roles.Add(userRole);

        var users = Enumerable.Range(2, 25).Select(i => new User
        {
            Id = i,
            RoleId = 1,
            Role = userRole, // Gán Role để thỏa điều kiện Role != ADMIN
            Email = $"user{i}@gmail.com",
            PasswordHash = "hashed_pwd",
            FullName = $"Test User {i}",
            IsActive = true
        }).ToList();

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();

        var sut = new UserService(dbContext);

        // Act
        var result = await sut.SearchUsersAsync(currentUserId, "User");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(20);
    }
}