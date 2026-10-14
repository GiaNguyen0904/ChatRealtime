using Messenger.Domain.Enums;
namespace Messenger.Application.Conversations.DTOs;

public class MessageDto
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } 
    public DateTime CreatedAt { get; set; }
    public List<SeenByDto> SeenBy { get; set; } = new();
}

public class SeenByDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
}