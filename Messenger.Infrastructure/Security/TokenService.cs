using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security;
using Messenger.Application.Auth.Interfaces;
using Messenger.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;


namespace Messenger.Infrastructure.Security
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>  // tạo danh sách các claim
            {
                new (JwtRegisteredClaimNames.Sub, user.Id.ToString()), // claim "sub" (subject) chứa ID của người dùng
                new (JwtRegisteredClaimNames.Email, user.Email), // claim "email" chứa địa chỉ email của người dùng
                new (ClaimTypes.Name, user.FullName), // claim "name" chứa tên đầy đủ của người dùng
                new (ClaimTypes.Role, user.Role.Name) // claim "role" chứa vai trò của người dùng
            };
            // Tạo khóa bảo mật từ chuỗi bí mật trong cài đặt JWT
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            // Tạo thông tin xác thực ký (signing credentials) sử dụng thuật toán HMAC SHA256
            var credentials = new SigningCredentials(
             key,
             SecurityAlgorithms.HmacSha256);

            // Tính toán thời gian hết hạn của token dựa trên cài đặt trong JwtSettings
            var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            // Tạo đối tượng JwtSecurityToken với các thông tin đã chuẩn bị
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            // Chuyển đổi đối tượng JwtSecurityToken thành chuỗi token và trả về
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetAccessTokenExpiration()
        {
            // Tính toán thời gian hết hạn của token dựa trên cài đặt trong JwtSettings
            return DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64); // Tạo một mảng byte ngẫu nhiên có độ dài 64
            
            return Convert.ToBase64String(randomBytes); // Chuyển đổi mảng byte thành chuỗi Base64 và trả về
        }

        public string HashRefreshToken(string refreshToken)
        {
            // Chuyển đổi chuỗi refresh token thành mảng byte
            var bytes = Encoding.UTF8.GetBytes(refreshToken);

            var hash= SHA256.HashData(bytes); // Tạo băm SHA256 từ mảng byte

            return Convert.ToHexString(hash); // Chuyển đổi mảng byte băm thành chuỗi hex và trả về
        }

        public DateTime GetRefreshTokenExpiration()
        {
            // Tính toán thời gian hết hạn của refresh token (7 ngày từ thời điểm hiện tại)
            return DateTime.UtcNow.AddDays(7);
        }
    }
}
