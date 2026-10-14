using FluentAssertions;
using Messenger.Application.Conversations.DTOs;
using Messenger.Application.Conversations.Services;
using Messenger.Application.Conversations.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Messenger.UnitTests.Application.Conversations;

public class ConversationServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    // ============================================================
    // GET CONVERSATIONS
    // ============================================================
    [Fact]
    public async Task GetConversationsAsync_ShouldReturnActiveConversationsWithCorrectUnreadCount()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        
        var conversation = new Conversation { Id = 1, Type = ConversationType.Group, Title = "Test Group", UpdatedAt = DateTime.UtcNow };
        dbContext.Conversations.Add(conversation);
        
        dbContext.ConversationMembers.Add(new ConversationMember 
        { 
            ConversationId = 1, UserId = 1, LeftAt = null, JoinedAt = DateTime.UtcNow.AddMinutes(-10), LastReadMessageId = 100 
        });

        // 1 tin nhắn cũ đã đọc, 2 tin nhắn mới chưa đọc
        dbContext.Messages.AddRange(
            new Message { Id = 100, ConversationId = 1, SenderId = 2, CreatedAt = DateTime.UtcNow.AddMinutes(-5), Content = "Old msg", MessageType = MessageType.Text },
            new Message { Id = 101, ConversationId = 1, SenderId = 2, CreatedAt = DateTime.UtcNow.AddMinutes(-2), Content = "New msg 1", MessageType = MessageType.Text },
            new Message { Id = 102, ConversationId = 1, SenderId = 2, CreatedAt = DateTime.UtcNow.AddMinutes(-1), Content = "New msg 2", MessageType =MessageType.Text }
        );

        await dbContext.SaveChangesAsync();
        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        var result = await sut.GetConversationsAsync(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(1);
        result.First().UnreadCount.Should().Be(2); // Tin nhắn 101 và 102
    }

    // ============================================================
    // OPEN PRIVATE CONVERSATION
    // ============================================================
    [Fact]
    public async Task OpenPrivateConversationAsync_ShouldReturnExistingId_WhenConversationExists()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        dbContext.Conversations.Add(new Conversation { Id = 1, DirectKey = "1_2", Type = ConversationType.Private });
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        var resultId = await sut.OpenPrivateConversationAsync(1, 2); // 1_2

        // Assert
        resultId.Should().Be(1);
        var count = await dbContext.Conversations.CountAsync();
        count.Should().Be(1); // Không tạo mới
    }

    [Fact]
    public async Task OpenPrivateConversationAsync_ShouldCreateNew_WhenConversationDoesNotExist()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        var resultId = await sut.OpenPrivateConversationAsync(2, 5); // 2_5

        // Assert
        var newConv = await dbContext.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == resultId);
        newConv.Should().NotBeNull();
        newConv!.DirectKey.Should().Be("2_5");
        newConv.Members.Should().HaveCount(2);
        newConv.Members.Should().Contain(m => m.UserId == 2);
        newConv.Members.Should().Contain(m => m.UserId == 5);
    }

    // ============================================================
    // CREATE GROUP
    // ============================================================
    [Fact]
    public async Task CreateGroupAsync_ShouldCreateGroupAndAssignRoles()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        var sut = new ConversationService(dbContext, mockNotificationService.Object);
        var dto = new CreateGroupDto { Title = "Dev Team", MemberIds = new List<int> { 2, 3 } };

        // Act
        var resultId = await sut.CreateGroupAsync(1, dto);

        // Assert
        var group = await dbContext.Conversations.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == resultId);
        group.Should().NotBeNull();
        group!.Type.Should().Be(ConversationType.Group);
        group.Members.Should().HaveCount(3); // 1 Creator + 2 Members
        group.Members.Single(m => m.UserId == 1).Role.Should().Be(ConversationMemberRole.Owner);
        group.Members.Single(m => m.UserId == 2).Role.Should().Be(ConversationMemberRole.Member);
    }

    // ============================================================
    // INVITE MEMBERS
    // ============================================================
    [Fact]
   
    public async Task InviteMemberAsync_ShouldThrowException_WhenNotMember()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        // KHÔNG add user 1 vào conversation_members để giả lập user không thuộc nhóm/không có quyền

        var sut = new ConversationService(dbContext, mockNotificationService.Object);
        var dto = new InviteMembersDto { MemberIds = new List<int> { 3 } };

        // Act
        Func<Task> act = async () => await sut.InviteMemberAsync(1, 1, dto);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    // ============================================================
    // TRANSFER OWNERSHIP
    // ============================================================
    [Fact]
    public async Task TransferOwnershipAsync_ShouldThrowException_WhenNotOwner()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 1, Role = ConversationMemberRole.Member, LeftAt = null });
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 2, Role = ConversationMemberRole.Member, LeftAt = null });
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        Func<Task> act = async () => await sut.TransferOwnershipAsync(1, 1, 2);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("You do not have permission to transfer ownership or the new owner is not a member of the conversation.");
    }

    [Fact]
    public async Task TransferOwnershipAsync_ShouldSwapRoles_WhenValid()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 1, Role = ConversationMemberRole.Owner, LeftAt = null });
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 2, Role = ConversationMemberRole.Member, LeftAt = null });
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        await sut.TransferOwnershipAsync(1, 1, 2);

        // Assert
        var oldOwner = await dbContext.ConversationMembers.FirstOrDefaultAsync(m => m.UserId == 1);
        var newOwner = await dbContext.ConversationMembers.FirstOrDefaultAsync(m => m.UserId == 2);
        
        oldOwner!.Role.Should().Be(ConversationMemberRole.Member);
        newOwner!.Role.Should().Be(ConversationMemberRole.Owner);
    }

    // ============================================================
    // LEAVE GROUP
    // ============================================================
    [Fact]
    public async Task LeaveGroupAsync_ShouldThrowException_WhenOwnerLeavesWithOtherMembers()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 1, Role = ConversationMemberRole.Owner, LeftAt = null });
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 2, Role = ConversationMemberRole.Member, LeftAt = null });
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        Func<Task> act = async () => await sut.LeaveGroupAsync(1, 1);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("You must transfer the Owner to others before leaving group");
    }

    [Fact]
    public async Task LeaveGroupAsync_ShouldSetLeftAt_WhenValidMemberLeaves()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        var member = new ConversationMember { ConversationId = 1, UserId = 1, Role = ConversationMemberRole.Member, LeftAt = null };
        dbContext.ConversationMembers.Add(member);
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);

        // Act
        await sut.LeaveGroupAsync(1, 1);

        // Assert
        var updatedMember = await dbContext.ConversationMembers.FindAsync(member.Id);
        updatedMember!.LeftAt.Should().NotBeNull();
    }

    // ============================================================
    // SEND MESSAGE
    // ============================================================
    [Fact]
    public async Task SendMessageAsync_ShouldCreateMessageAndUpdateConversation()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        dbContext.Conversations.Add(new Conversation { Id = 1, UpdatedAt = DateTime.UtcNow.AddDays(-1) });
        dbContext.ConversationMembers.Add(new ConversationMember { ConversationId = 1, UserId = 1, LeftAt = null });
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);
        var dto = new SendMessageDto { Content = "Hello", MessageType = MessageType.Text }; // Giả định enum MessageType.TEXT

        // Act
        var result = await sut.SendMessageAsync(1, 1, dto);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be("Hello");
        
        var conv = await dbContext.Conversations.FindAsync(1);
        conv!.UpdatedAt.Date.Should().Be(DateTime.UtcNow.Date); // UpdatedAt đã bị thay đổi

        var member = await dbContext.ConversationMembers.FirstOrDefaultAsync(m => m.UserId == 1);
        member!.LastReadMessageId.Should().Be(result.Id); // Tự động đánh dấu đã xem
    }

    // ============================================================
    // MARK AS READ
    // ============================================================
    [Fact]
    public async Task MarkAsReadAsync_ShouldUpdateLastReadMessageId()
    {
        // Arrange
        using var dbContext = GetInMemoryDbContext();
        var mockNotificationService = new Mock<IChatNotificationService>();
        var member = new ConversationMember { ConversationId = 1, UserId = 1, LeftAt = null, LastReadMessageId = 10 };
        dbContext.ConversationMembers.Add(member);
        await dbContext.SaveChangesAsync();

        var sut = new ConversationService(dbContext, mockNotificationService.Object);
        var dto = new MarkReadDto { LastReadMessageId = 20 };

        // Act
        await sut.MarkAsReadAsync(1, 1, dto);

        // Assert
        var updatedMember = await dbContext.ConversationMembers.FindAsync(member.Id);
        updatedMember!.LastReadMessageId.Should().Be(20);
    }
}