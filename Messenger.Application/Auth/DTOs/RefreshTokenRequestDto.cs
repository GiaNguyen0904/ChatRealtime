using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Application.Auth.DTOs
{
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; }= string.Empty;
    }
}
