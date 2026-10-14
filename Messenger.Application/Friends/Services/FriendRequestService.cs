using Messenger.Application.Common;
using Messenger.Application.Common.ConflictException.cs;
using Messenger.Application.Common.Exceptions;
using Messenger.Application.Common.Interfaces;
using Messenger.Application.Friends.DTOs;
using Messenger.Application.Friends.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Messenger.Application.Friends.Services
{
    public class FriendRequestService : IFriendRequestService 
    {
        private readonly IApplicationDbContext _context;
        public FriendRequestService(IApplicationDbContext context)
        {
            _context = context;
        }
        
          public async Task SendRequestAsync(int currentUserId, SendFriendRequestDto requestDto)
        {
            // check xem nguoi gui co la nguoi nhan ko
            if(currentUserId == requestDto.ReceiverId)
            {
                throw new BadRequestException("You can't send request to myself");
            }
            // check nguoi gui co ton tai ko 
            var receiverExist = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == requestDto.ReceiverId && u.IsActive);

            if(receiverExist == null)
            {
                throw new NotFoundException("Receiver not found or being not active");
            }

            // rule check user_id < friend_id
            var minId = Math.Min(currentUserId, requestDto.ReceiverId);
            var maxId = Math.Max(currentUserId, requestDto.ReceiverId);

            // check if they are already friends
            var isAlrealdyFriend = await _context.Friendships
                .AnyAsync(f => f.UserId == minId && f.FriendId == maxId);

            if (isAlrealdyFriend)
            {
                throw new ConflictException("You are already friends with this user");
            }

            // check status pending 
            // neu da co loi moi ket ban dang cho phep, thi ko cho gui them
            var hasPendingReq = await _context.FriendRequests
                .AnyAsync(r => r.Status == FriendRequestStatus.Pending &&
                ((r.SenderId == currentUserId && r.ReceiverId == requestDto.ReceiverId) ||
                (r.SenderId == requestDto.ReceiverId && r.ReceiverId == currentUserId)));

            if (hasPendingReq)
            {
                throw new ConflictException("There is already a pending friend request between you and this user");
            }

            var friendRequest = new FriendRequest
            {
                SenderId = currentUserId,
                ReceiverId = requestDto.ReceiverId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            // luu vao db
            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();

        }

        public async Task<List<FriendRequestDto>> GetRequestAsync(int currentUserId, string type)
        {
            // lay danh sach cac loi moi ket ban theo type
            var query = _context.FriendRequests
                .AsNoTracking()
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Where(r => r.Status == FriendRequestStatus.Pending);
            // filter theo type
            if (string.Equals(type,"sent", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.SenderId == currentUserId); // lay cac loi moi ket ban da gui
            }
            else
            {
                query=query.Where(r => r.ReceiverId == currentUserId); // lay cac loi moi ket ban da nhan
            }

            // map to dto
            return await query.Select(r => new FriendRequestDto
            {
                Id = r.Id,
                SenderId = r.SenderId,
                SenderFullName = r.Sender.FullName,
                SenderEmail = r.Sender.Email,
                ReceiverId = r.ReceiverId,
                ReceiverFullName = r.Receiver.FullName,
                ReceiverEmail = r.Receiver.Email,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToListAsync();
        }

        public async Task RespondRequestAsync(int currentUserId,int requestId, RespondFriendRequestDto dto)
        {
            // neu status la pending thi khong hop le 
            if(dto.Status == FriendRequestStatus.Pending)
            {
                throw new BadRequestException("The respond status is invalid");
            }
            // lay loi moi ket ban theo reqId ,
            // chi lay loi moi ket ban dang pending va nguoi nhan la currentUserId
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId && r.Status == FriendRequestStatus.Pending);
            // khong co req, quang loi loi moi ko ton tai hoac da duoc xu ly
            if(request == null)
            {
                throw new NotFoundException("Friend request is not existed or has been processed");
            }
            // gan status va thoi gian tao
            request.Status = dto.Status;
            request.UpdatedAt = DateTime.UtcNow;

            // neu status la accepted thi tao moi quan he bb
            if(dto.Status == FriendRequestStatus.Accepted)
            {
                // chon min,max dua theo userId va friendId
                var minId = Math.Min(request.SenderId, request.ReceiverId); 
                var maxId = Math.Max(request.SenderId, request.ReceiverId);

                var friendShip = new Friendship
                {
                    UserId = minId,
                    FriendId = maxId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Friendships.Add(friendShip); // them vao db
            }

            await _context.SaveChangesAsync();


        }



    }
}
