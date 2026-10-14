using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Application.Auth.DTOs;
namespace Messenger.Application.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequestDto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequestDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto RefreshTokenRequestDto);
        Task LogoutAsync(LogoutRequestDto logoutRequestDto);
        Task ChangePasswordAsync(int UserId, ChangePasswordRequestDto changePasswordRequestDto);
    }
}
