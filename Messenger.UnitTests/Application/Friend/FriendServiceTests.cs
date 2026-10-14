using FluentAssertions;
using Messenger.Application.Common;
using Messenger.Application.Common.ConflictException.cs;
using Messenger.Application.Common.Exceptions;
using Messenger.Application.Friends.DTOs;
using Messenger.Application.Friends.Services;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Messenger.UnitTests.Application.Friends;

public class FriendRequestServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // ============================================================
    // SEND REQUEST
    // ============================================================

    [Fact]
    public async Task SendRequestAsync_ShouldThrowBadRequest_WhenSendingToSelf()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var sut = new FriendRequestService(dbContext);
        var dto = new SendFriendRequestDto { ReceiverId = 1 };

        // Act
        Func<Task> act = async () => await sut.SendRequestAsync(1, dto);

        // Assert
        await act.Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("You can't send request to myself");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldThrowNotFound_WhenReceiverDoesNotExistOrIsInactive()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.Users.Add(new User
        {
            Id = 2,
            Email = "inactive@gmail.com",
            PasswordHash = "hashed_pwd",
            FullName = "Inactive User",
            IsActive = false
        });
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new SendFriendRequestDto { ReceiverId = 2 };

        // Act
        Func<Task> act = async () => await sut.SendRequestAsync(1, dto);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Receiver not found or being not active");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldThrowConflict_WhenAlreadyFriends()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.Users.AddRange(
            new User { Id = 1, Email = "user1@gmail.com", PasswordHash = "hashed_pwd", FullName = "User One", IsActive = true },
            new User { Id = 2, Email = "user2@gmail.com", PasswordHash = "hashed_pwd", FullName = "User Two", IsActive = true }
        );

        dbContext.Friendships.Add(new Friendship { Id = 1, UserId = 1, FriendId = 2, CreatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new SendFriendRequestDto { ReceiverId = 2 };

        // Act
        Func<Task> act = async () => await sut.SendRequestAsync(1, dto);

        // Assert
        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("You are already friends with this user");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldThrowConflict_WhenPendingRequestAlreadyExists()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.Users.AddRange(
            new User { Id = 1, Email = "user1@gmail.com", PasswordHash = "hashed_pwd", FullName = "User One", IsActive = true },
            new User { Id = 2, Email = "user2@gmail.com", PasswordHash = "hashed_pwd", FullName = "User Two", IsActive = true }
        );

        // Đã có lời mời kết bạn theo chiều ngược lại đang Pending
        dbContext.FriendRequests.Add(new FriendRequest
        {
            Id = 1,
            SenderId = 2,
            ReceiverId = 1,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new SendFriendRequestDto { ReceiverId = 2 };

        // Act
        Func<Task> act = async () => await sut.SendRequestAsync(1, dto);

        // Assert
        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("There is already a pending friend request between you and this user");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldCreateFriendRequest_WhenValidRequest()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.Users.AddRange(
            new User { Id = 1, Email = "user1@gmail.com", PasswordHash = "hashed_pwd", FullName = "User One", IsActive = true },
            new User { Id = 2, Email = "user2@gmail.com", PasswordHash = "hashed_pwd", FullName = "User Two", IsActive = true }
        );
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new SendFriendRequestDto { ReceiverId = 2 };

        // Act
        await sut.SendRequestAsync(1, dto);

        // Assert
        var createdRequest = await dbContext.FriendRequests.FirstOrDefaultAsync();
        createdRequest.Should().NotBeNull();
        createdRequest!.SenderId.Should().Be(1);
        createdRequest.ReceiverId.Should().Be(2);
        createdRequest.Status.Should().Be(FriendRequestStatus.Pending);
    }

    // ============================================================
    // GET REQUESTS
    // ============================================================

    [Fact]
    public async Task GetRequestAsync_ShouldReturnSentPendingRequests_WhenTypeIsSent()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var sender = new User { Id = 1, Email = "sender@gmail.com", PasswordHash = "hashed_pwd", FullName = "Sender User", IsActive = true };
        var receiver = new User { Id = 2, Email = "receiver@gmail.com", PasswordHash = "hashed_pwd", FullName = "Receiver User", IsActive = true };

        dbContext.Users.AddRange(sender, receiver);
        dbContext.FriendRequests.AddRange(
            new FriendRequest { Id = 1, SenderId = 1, ReceiverId = 2, Status = FriendRequestStatus.Pending, Sender = sender, Receiver = receiver },
            new FriendRequest { Id = 2, SenderId = 2, ReceiverId = 1, Status = FriendRequestStatus.Pending, Sender = receiver, Receiver = sender },
            new FriendRequest { Id = 3, SenderId = 1, ReceiverId = 2, Status = FriendRequestStatus.Accepted, Sender = sender, Receiver = receiver }
        );
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);

        // Act
        var result = await sut.GetRequestAsync(1, "sent");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(1);
        result.First().ReceiverFullName.Should().Be("Receiver User");
    }

    [Fact]
    public async Task GetRequestAsync_ShouldReturnReceivedPendingRequests_WhenTypeIsNotSent()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        var sender = new User { Id = 2, Email = "sender@gmail.com", PasswordHash = "hashed_pwd", FullName = "Sender User", IsActive = true };
        var receiver = new User { Id = 1, Email = "receiver@gmail.com", PasswordHash = "hashed_pwd", FullName = "Receiver User", IsActive = true };

        dbContext.Users.AddRange(sender, receiver);
        dbContext.FriendRequests.Add(
            new FriendRequest { Id = 1, SenderId = 2, ReceiverId = 1, Status = FriendRequestStatus.Pending, Sender = sender, Receiver = receiver }
        );
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);

        // Act
        var result = await sut.GetRequestAsync(1, "received");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(1);
        result.First().SenderFullName.Should().Be("Sender User");
    }

    // ============================================================
    // RESPOND REQUEST
    // ============================================================

    [Fact]
    public async Task RespondRequestAsync_ShouldThrowBadRequest_WhenStatusIsPending()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var sut = new FriendRequestService(dbContext);
        var dto = new RespondFriendRequestDto { Status = FriendRequestStatus.Pending };

        // Act
        Func<Task> act = async () => await sut.RespondRequestAsync(1, 10, dto);

        // Assert
        await act.Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("The respond status is invalid");
    }

    [Fact]
    public async Task RespondRequestAsync_ShouldThrowNotFound_WhenRequestDoesNotExistOrNotPending()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var sut = new FriendRequestService(dbContext);
        var dto = new RespondFriendRequestDto { Status = FriendRequestStatus.Accepted };

        // Act
        Func<Task> act = async () => await sut.RespondRequestAsync(1, 99, dto);

        // Assert
        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Friend request is not existed or has been processed");
    }

    [Fact]
    public async Task RespondRequestAsync_ShouldAcceptRequestAndCreateFriendship_WhenAccepted()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.FriendRequests.Add(new FriendRequest
        {
            Id = 10,
            SenderId = 5,
            ReceiverId = 1,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new RespondFriendRequestDto { Status = FriendRequestStatus.Accepted };

        // Act
        await sut.RespondRequestAsync(1, 10, dto);

        // Assert
        var updatedRequest = await dbContext.FriendRequests.FindAsync(10);
        updatedRequest!.Status.Should().Be(FriendRequestStatus.Accepted);

        var createdFriendship = await dbContext.Friendships.FirstOrDefaultAsync();
        createdFriendship.Should().NotBeNull();
        createdFriendship!.UserId.Should().Be(1); // Min(5, 1) = 1
        createdFriendship.FriendId.Should().Be(5); // Max(5, 1) = 5
    }

    [Fact]
    public async Task RespondRequestAsync_ShouldRejectRequestWithoutCreatingFriendship_WhenRejected()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();

        dbContext.FriendRequests.Add(new FriendRequest
        {
            Id = 10,
            SenderId = 5,
            ReceiverId = 1,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var sut = new FriendRequestService(dbContext);
        var dto = new RespondFriendRequestDto { Status = FriendRequestStatus.Rejected };

        // Act
        await sut.RespondRequestAsync(1, 10, dto);

        // Assert
        var updatedRequest = await dbContext.FriendRequests.FindAsync(10);
        updatedRequest!.Status.Should().Be(FriendRequestStatus.Rejected);

        var friendshipsCount = await dbContext.Friendships.CountAsync();
        friendshipsCount.Should().Be(0);
    }
}