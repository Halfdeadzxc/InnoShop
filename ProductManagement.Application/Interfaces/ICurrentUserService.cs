using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagement.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Email { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }

        Guid GetRequiredUserId();
        string GetRequiredEmail();
        string GetRequiredRole();
        bool IsInRole(string role);
    }
}
