namespace Messenger.Application.Conversations.DTOs;

public class CreateGroupDto
{
    public string Title { get; set; } = string.Empty;
    public List<int> MemberIds { get; set; } = new();
}