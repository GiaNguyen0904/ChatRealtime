using Messenger.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Entities
{
    public class Message
    {
        public long Id { get; set; }

        public int ConversationId { get; set; }

        public int SenderId { get; set; }

        public string Content { get; set; } = null!;

        public MessageType MessageType { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Conversation Conversation { get; set; } = null!;

        public User Sender { get; set; } = null!;
    }
}
