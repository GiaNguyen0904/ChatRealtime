using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Users.DTOs;

namespace Messenger.Application.Users.Interfaces
{
   public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(int currentUserId);
        Task<List<UserSearchDto>> SearchUsersAsync(int currentUserId, string keyword);
    }
}
