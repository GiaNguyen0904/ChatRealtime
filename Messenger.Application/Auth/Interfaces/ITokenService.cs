using Messenger.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Application.Auth.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        DateTime GetAccessTokenExpiration();

        string GenerateRefreshToken();

        string HashRefreshToken(string refreshToken);

        DateTime GetRefreshTokenExpiration();
    }
}
