using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Messenger.Api.Hubs;
using Messenger.Application.Conversations.DTOs;
using Messenger.Application.Conversations.Interfaces;
using Messenger.Application.Common.Interfaces; // Quan trọng: Để nhận diện IApplicationDbContext

namespace Messenger.Api.Services;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IApplicationDbContext _dbContext; // Inject thêm DB

    public ChatNotificationService(IHubContext<ChatHub> hubContext, IApplicationDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    public async Task SendNewMessageNotificationAsync(int conversationId, MessageDto messageDto)
    {
        // 1. Quét Database xem nhóm chat này đang có những ai
        var memberIds = await _dbContext.ConversationMembers
            .Where(cm => cm.ConversationId == conversationId && cm.LeftAt == null)
            .Select(cm => cm.UserId)
            .ToListAsync();

        // 2. Đi gõ cửa từng "phòng cá nhân" và phát tin nhắn
        foreach (var userId in memberIds)
        {
            await _hubContext.Clients.Group($"User_{userId}")
                .SendAsync("ReceiveNewMessage", conversationId, messageDto); 
        }
    }

    public async Task SendTypingNotificationAsync(int conversationId, int userId, bool isTyping)
    {
        // 1. Kiểm tra xem người gửi có thuộc cuộc trò chuyện này không
        var isMember = await _dbContext.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId && cm.LeftAt == null);

        if (!isMember) return;

        // 2. Lấy danh sách thành viên còn lại (ngoại trừ người đang gõ)
        var otherMemberIds = await _dbContext.ConversationMembers
            .Where(cm => cm.ConversationId == conversationId && cm.UserId != userId && cm.LeftAt == null)
            .Select(cm => cm.UserId)
            .ToListAsync();

        // 3. Gửi sự kiện UserTyping tới từng thành viên
        foreach (var targetUserId in otherMemberIds)
        {
            await _hubContext.Clients.Group($"User_{targetUserId}")
                .SendAsync("UserTyping", conversationId, userId, isTyping);
        }
    }
}