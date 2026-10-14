using Messenger.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Entities
{
    public class ConversationMember
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public int UserId { get; set; }

        public ConversationMemberRole Role { get; set; }

        public DateTime JoinedAt { get; set; }

        public DateTime? LeftAt { get; set; }
        public long? LastReadMessageId { get; set; }
        public string? Nickname {get;set; }

        // Navigation
        public Conversation Conversation { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}
