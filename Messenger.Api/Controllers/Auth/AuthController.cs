using Messenger.Application.Auth.Interfaces;
using Messenger.Application.Auth.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Messenger.Application.Common;
namespace Messenger.Api.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto registerRequestDto)
        {
            var result = await _authService.RegisterAsync(registerRequestDto);
            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto loginRequestDto)
        {
            var result = await _authService.LoginAsync(loginRequestDto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenRequestDto);
            return Ok(result);
        }
        
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequestDto logoutRequestDto)
        {
             // goi cho service
            await _authService.LogoutAsync(logoutRequestDto);
            return Ok(new
            {
                message = "Logout successful" 
            }); // kem theo message 
        }

        [Authorize] // 1. Yêu cầu Request phải chứa Access Token hợp lệ (đã đăng nhập) thì mới cho truy cập API này
        [HttpPost("change-password")] // 2. Định nghĩa Phương thức HTTP là POST và Endpoint là: /api/auth/change-password
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto changePasswordRequestDto)
        {
            // 3. Trích xuất Claim chứa ID của người dùng từ Token (trong Payload của JWT)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            // 4. Kiểm tra nếu không tìm thấy Claim ID (Token bị thiếu thông tin hoặc không hợp lệ) thì báo lỗi 401
            if (userIdClaim == null)
            {
                throw new UnauthorizedException("User identity not found");
            }

            // 5. Chuyển đổi ID dạng chuỗi (String) từ Token sang dạng số nguyên (Integer) để xử lý trong DB
            var userId = int.Parse(userIdClaim.Value);

            // 6. Gọi tầng Service xử lý nghiệp vụ
            await _authService.ChangePasswordAsync(userId, changePasswordRequestDto);

            // tra ve respone http status 
            return Ok(new
            {
                message = "Password changed successfully."
            });
        }

    }
}
