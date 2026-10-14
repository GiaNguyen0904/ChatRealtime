namespace Messenger.Application.Friends.DTOs;

public class FriendDto
{
    public int FriendshipId { get; set; }
    public int FriendId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}