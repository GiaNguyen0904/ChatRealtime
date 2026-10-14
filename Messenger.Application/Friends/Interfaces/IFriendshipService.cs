using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Friends.DTOs;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;

namespace Messenger.Application.Friends.Interfaces
{
    public interface IFriendshipService
    {
        Task<List<FriendDto>> GetFriendsAsync(int currentUserId);
        Task DeleteFriendAsync(int currentUserId, int friendId);
    }
}
