namespace Messenger.Application.Conversations.DTOs
{
    public class ChangeNicknameDto
    {
        public int TargetUserId { get; set; } // ID của người được đổi biệt danh
        public string? NewNickname { get; set; } // Biệt danh mới (truyền null hoặc "" nếu muốn gỡ biệt danh)
    }
}