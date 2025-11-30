using System.Security.Claims;
using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
        Guid? ValidateTokenAndGetUserId(string token);
        string GenerateRefreshToken();
    }
}
