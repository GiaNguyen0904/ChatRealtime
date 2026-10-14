using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Role Role { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();

        public ICollection<FriendRequest> SentFriendRequests { get; set; }
            = new List<FriendRequest>();

        public ICollection<FriendRequest> ReceivedFriendRequests { get; set; }
            = new List<FriendRequest>();

        public ICollection<Friendship> FriendshipsAsUser { get; set; }
            = new List<Friendship>();

        public ICollection<Friendship> FriendshipsAsFriend { get; set; }
            = new List<Friendship>();

        public ICollection<ConversationMember> ConversationMemberships { get; set; }
            = new List<ConversationMember>();

        public ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
