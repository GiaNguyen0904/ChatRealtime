using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Auth.Interfaces;
using Messenger.Application.Auth.DTOs;
using Messenger.Application.Common.Interfaces;
using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Messenger.Application.Common.ConflictException.cs;
using Messenger.Application.Common;

namespace Messenger.Application.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(
            IApplicationDbContext context,
            ITokenService tokenService,
            IPasswordHasher passwordHasher
        )
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // input validation
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new ArgumentException("Full name is required.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var fullName = request.FullName.Trim();

            // Check if user with the same email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                throw new ConflictException("Email is already registered.");
            }

            // lấy đối tượng Role với tên "USER" từ cơ sở dữ liệu
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "USER");


            if (userRole == null)
            {
                throw new InvalidOperationException("User role not found.");
            }

            // hash the password
            var hashedPassword = _passwordHasher.Hash(request.Password);

            //create user 
            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword,
                FullName = fullName,
                RoleId = userRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // gán đối tượng Role cho người dùng mới tạo, để đảm bảo rằng thông tin vai trò
            // được bao gồm trong đối tượng người dùng khi tạo token
            user.Role = userRole;

            // sinh access token cho người dùng mới tạo
            var accessToken = _tokenService.GenerateAccessToken(user);
            var expiration = _tokenService.GetAccessTokenExpiration();
            var refreshToken = await CreateRefreshTokenAsync(user);// sinh refresh token cho người dùng mới tạo

            await _context.SaveChangesAsync(); // lưu các thay đổi vào cơ sở dữ liệu
            //return response
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiration,
            };

        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            // kiem tra xem email co null ko
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.");
            }
            // kiem tra password co null ko 
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.");
            }
            // xu ly email
            var email = request.Email
                .Trim()
                .ToLowerInvariant();
            // 
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new UnauthorizedException(
                    "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedException(
                    "User account is inactive.");
            }

            var isPasswordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new UnauthorizedException(
                    "Invalid email or password.");
            }

            // tao access token cho nguoi dung
            var accessToken = _tokenService.GenerateAccessToken(user);
            // tao refresh token cho nguoi dung
            var refreshToken = await CreateRefreshTokenAsync(user);
            // lay thoi gian het han cua access token
            var expiration = _tokenService.GetAccessTokenExpiration();

            await _context.SaveChangesAsync(); // luu thay doi vao db

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiration
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            // kiem tra co token hay ko
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new UnauthorizedException("Refresh token is required");
            }

            // hash token ban dau de so sanh voi token trong db
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

            // lay token tu db
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User) // include user tuong ung voi token
                .ThenInclude(u => u.Role) // include role tuong ung voi user
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            //  check token co ton tai k
            if (refreshToken == null)
            {
                throw new UnauthorizedException(
                    "Refresh token has been revoked.");
            }

            // check token da bi revoken chua
            if (refreshToken.RevokedAt.HasValue)
            {
                throw new UnauthorizedException(
                     "Refresh token has been revoked.");
            }
            // check token het han
            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException(
                    "Refresh token has expired.");
            }
            // check user co active k
            if (!refreshToken.User.IsActive)
            {
                throw new UnauthorizedException(
                    "User account is inactive.");
            }
            // revoken token cu
            refreshToken.RevokedAt = DateTime.UtcNow;
            //  tao refreshtoken moi
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // hash refreshtoken moi
            var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);

            var newRefreshTokenExpiration = _tokenService.GetRefreshTokenExpiration();

            var refreshEntity = new RefreshToken // tao doi tuong refreshToken moi de luu vao db
            {
                UserId = refreshToken.UserId,
                TokenHash = newRefreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = newRefreshTokenExpiration
            };

            await _context.RefreshTokens.AddAsync(refreshEntity); // them doi tuong refreshToken moi vao db

            // tao accessToken moi 
            var accessToken = _tokenService.GenerateAccessToken(refreshToken.User);

            // tao thoi gian cho accessToken moi
            var accessTokenExpiration =
                _tokenService.GetAccessTokenExpiration();

            await _context.SaveChangesAsync(); // luu thay doi vao db

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = accessTokenExpiration
            };
        }

        private async Task<string> CreateRefreshTokenAsync(User user) // tạo refresh token mới cho người dùng
        {
            var refreshToken = _tokenService.GenerateRefreshToken(); // tạo refresh token mới


            // hash refresh token mới
            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);

            // lấy thời gian hết hạn của refresh token mới
            var expiration = _tokenService.GetRefreshTokenExpiration();

            // tạo đối tượng RefreshToken mới để lưu vào cơ sở dữ liệu
            var entity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiration
            };

            await _context.RefreshTokens.AddAsync(entity); // thêm đối tượng RefreshToken mới vào cơ sở dữ liệu

            return refreshToken;
        }

        public async Task LogoutAsync(LogoutRequestDto request)
        {
            // kiem tra xem refresh token co null hay ko
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new UnauthorizedException("Refresh token is required.");
            }

            // hash refresh token de so sanh voi token trong db
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            // lay token tu db
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            // check xem token co ton tai hay ko
            if (refreshToken == null)
            {
                throw new UnauthorizedException("Refresh token is invalid.");
            }

            // check xem token da bi revoken hay chua
            if (refreshToken.RevokedAt.HasValue)
            {
                throw new UnauthorizedException("Refresh token has already been revoked.");
            }

            refreshToken.RevokedAt = DateTime.UtcNow; // revoken token

            await _context.SaveChangesAsync(); // luu thay doi vao db
        }

        public async Task ChangePasswordAsync (int UserId, ChangePasswordRequestDto request)
        {
            // check mat khau cu co null hay ko
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                throw new UnauthorizedException("Current password is required.");
            }

            // check mat khau moi co null hay ko
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new UnauthorizedException("New password is required.");
            }

            // check mat khau moi co trung voi mat khau cu hay ko
            if(request.CurrentPassword == request.NewPassword)
            {
                throw new ArgumentException("New password must be different from current password.");
            }

            // lay user tu db
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null)
            {
                throw new UnauthorizedException("User not found.");
            }

            // check user co ton tai hay ko
            if (!user.IsActive)
            {
                throw new UnauthorizedException("User account is inactive.");
            }

            // check mat khau cu co dung hay ko
            var isPasswordValid = _passwordHasher.Verify(
                request.CurrentPassword,
                user.PasswordHash);
            // 
            if(!isPasswordValid)
            {
                throw new UnauthorizedException("Current password is incorrect.");
            }
            // hash mat khau moi
            var hashedNewPassword = _passwordHasher.Hash(request.NewPassword);
            // gan mat khau moi cho user 
            user.PasswordHash = hashedNewPassword;
            user.UpdatedAt= DateTime.UtcNow;
            // revoken tat ca refresh token cua user
            var userRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt== null)
                .ToListAsync();

            foreach(var refreshtoken in userRefreshTokens)
            {
                refreshtoken.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(); // luu thay doi vao db
        }

    }
}
