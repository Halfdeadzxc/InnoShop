using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Application.Exceptions
{
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException() : base("You are not authorized to access this resource.") { }
        public UnauthorizedException(string message) : base(message) { }
    }
}
