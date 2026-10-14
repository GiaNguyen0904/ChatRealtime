using Messenger.Application.Conversations.DTOs;

namespace Messenger.Application.Conversations.Interfaces;

public interface IChatNotificationService
{
    Task SendNewMessageNotificationAsync(int conversationId, MessageDto messageDto);
    Task SendTypingNotificationAsync(int conversationId, int userId, bool isTyping);
}

