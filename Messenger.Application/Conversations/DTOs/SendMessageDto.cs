using Messenger.Domain.Enums;

namespace Messenger.Application.Conversations.DTOs;
public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } 
}