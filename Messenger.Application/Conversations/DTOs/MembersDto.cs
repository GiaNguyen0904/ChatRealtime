
 namespace Messenger.Application.Conversations.DTOs;
 public class MemberDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }= string.Empty;
    public string Role { get; set; }= string.Empty;
    public string? Nickname {get; set;}
}