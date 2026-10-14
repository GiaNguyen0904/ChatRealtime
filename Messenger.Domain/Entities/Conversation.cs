using Messenger.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Entities
{
    public class Conversation
    {
        public int Id { get; set; }

        public ConversationType Type { get; set; }

        public string? Title { get; set; }

        public string? DirectKey { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<ConversationMember> Members { get; set; }
            = new List<ConversationMember>();

        public ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
