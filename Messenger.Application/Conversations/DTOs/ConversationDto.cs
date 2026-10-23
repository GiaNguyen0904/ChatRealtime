using Messenger.Domain.Enums;
namespace Messenger.Application.Conversations.DTOs;

public class ConversationDto
{
    public int Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Title { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UnreadCount { get; set; } // đếm số tin nhắn chưa đọc
    public int? OtherUserId { get; set; } // Id của người đối thoại (dành cho Chat Private)
}