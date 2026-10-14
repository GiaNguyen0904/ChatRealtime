using Messenger.Application.Users.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Users.Services;
using Messenger.Application.Common.Interfaces;
using Messenger.Application.Users.DTOs;
using Microsoft.EntityFrameworkCore;
using Messenger.Application.Common;
namespace Messenger.Application.Users.Services
{
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _context;
        public UserService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto> GetProfileAsync(int currentUserId)
        {
            // tim user
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId && u.IsActive);

            // neu ko co user, quang ra loi 
            if (user == null)
            {
                throw new NotFoundException("User not found or being not active");
            }

            // tra lai res cho client
            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
            };

        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(int currentUserId, string keyword)
        {
            // Nếu keyword trống hoặc rỗng, trả về danh sách user mặc định (không lọc theo từ khóa)
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id != currentUserId && u.IsActive && u.Role.Name != "ADMIN")
                    .Take(20)
                    .Select(u => new UserSearchDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FullName = u.FullName
                    }).ToListAsync();
            }

            var searchTerm = keyword.Trim().ToLower();

            // Nếu có từ khóa, thực hiện lọc theo tên hoặc email
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != currentUserId && u.IsActive
                        && (u.FullName.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm)) && u.Role.Name != "ADMIN")
                .Take(20)
                .Select(u => new UserSearchDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName
                }).ToListAsync();
        }
    }
}
