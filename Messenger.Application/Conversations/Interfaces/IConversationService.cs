using Messenger.Application.Conversations.DTOs;
namespace Messenger.Application.Conversations.Interfaces;

public interface IConversationService
{
    Task<IEnumerable<ConversationDto>> GetConversationsAsync( int currentUserId);
    Task<int> OpenPrivateConversationAsync( int currentUserId, int friendId);
    Task<int> CreateGroupAsync(int currentUserId, CreateGroupDto createGroupDto);
    Task InviteMemberAsync (int currentUserId, int conversationId, InviteMembersDto inviteMembersDto);
    Task TransferOwnershipAsync( int currentUserId, int conversationId, int newOwnerId);
    Task LeaveGroupAsync ( int currentUserId, int conversationId);
    Task<IEnumerable<MessageDto>> GetMessageAsync(int currentUserId,int conversationId, 
    int page, int pageSize);
    Task<MessageDto> SendMessageAsync ( int currentUserId, int conversationId, 
    SendMessageDto sendMessageDto);
    Task MarkAsReadAsync ( int currentUserId, int conversationId, MarkReadDto markReadDto);
    Task<IEnumerable<MemberDto>> GetMembersAsync(int conversationId);
    Task ChangeNicknameAsync( int currentUserId,int conversationId, ChangeNicknameDto dto);
    Task ChangeGroupTitleAsync (int currentUserId, int conversationId, ChangeGroupTitleDto dto);



}