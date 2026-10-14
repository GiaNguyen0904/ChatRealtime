using Messenger.Domain.Enums;

namespace Messenger.Application.Friends.DTOs;

public class RespondFriendRequestDto
{
    public FriendRequestStatus Status { get; set; }
}