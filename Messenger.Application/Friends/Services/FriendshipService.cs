using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Common.Exceptions;
using Messenger.Application.Friends.DTOs;
using Messenger.Application.Friends.Interfaces;
using Microsoft.EntityFrameworkCore;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Application.Common;
using Messenger.Application.Common.ConflictException.cs;
using Messenger.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Internal;

namespace Messenger.Application.Friends.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IApplicationDbContext _context;
        public FriendshipService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FriendDto>> GetFriendsAsync(int currentUserId)
        {
            return await _context.Friendships
                .AsNoTracking()
                .Where(f => f.UserId == currentUserId || f.FriendId == currentUserId)
                .Select(f => new FriendDto
                {
                    FriendshipId = f.Id,
                    FriendId = f.UserId == currentUserId ? f.FriendId : f.UserId,
                    FullName = f.UserId == currentUserId ? f.Friend.FullName : f.User.FullName,
                    Email = f.UserId == currentUserId ? f.Friend.Email : f.User.Email,
                    CreatedAt = f.CreatedAt
                }).ToListAsync();
        }

        public async Task DeleteFriendAsync(int currentUserId, int friendId)
        {
            var minId = Math.Min(currentUserId, friendId);
            var maxId = Math.Max(currentUserId, friendId);

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.UserId == minId && f.FriendId == maxId);
            if(friendship == null)
            {
                throw new NotFoundException("Friendship is not existed");
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
        }
    }
}
