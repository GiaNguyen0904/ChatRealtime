using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Application.Common;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message) { }
}
