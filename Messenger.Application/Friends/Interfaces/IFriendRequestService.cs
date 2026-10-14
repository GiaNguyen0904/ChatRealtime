using Messenger.Application.Friends.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;

namespace Messenger.Application.Friends.Interfaces
{
    public interface IFriendRequestService
    {
        Task SendRequestAsync(int currentUserId, SendFriendRequestDto requestDto); // gui loi moi ket ban
        Task<List<FriendRequestDto>> GetRequestAsync(int currentUserId, string type);
        Task RespondRequestAsync(int currentUserId, int requestId, RespondFriendRequestDto requestDto);
    }
}
