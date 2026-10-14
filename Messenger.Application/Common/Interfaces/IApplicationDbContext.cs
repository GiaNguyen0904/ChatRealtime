using Messenger.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<FriendRequest> FriendRequests { get; }
        DbSet<Friendship> Friendships { get; }

        DbSet<Conversation> Conversations { get; }
        DbSet<ConversationMember> ConversationMembers { get; }
        DbSet<Message> Messages { get; }
        

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
