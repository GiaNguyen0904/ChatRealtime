using Messenger.Domain.Enums;

namespace Messenger.Application.Friends.DTOs;

public class FriendRequestDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderFullName { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public int ReceiverId { get; set; }
    public string ReceiverFullName { get; set; } = null!;
    public string ReceiverEmail { get; set; } = null!;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}