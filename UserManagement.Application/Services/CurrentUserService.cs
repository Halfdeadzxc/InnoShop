using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Interfaces;

namespace UserManagement.Application.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(userIdClaim, out var id) ? id : null;
            }
        }

        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAuthenticated => UserId.HasValue;

        public Guid GetRequiredUserId()
        {
            return UserId ?? throw new UnauthorizedException("User is not authenticated");
        }

        public string GetRequiredEmail()
        {
            return Email ?? throw new UnauthorizedException("User email not found");
        }

        public string GetRequiredRole()
        {
            return Role ?? throw new UnauthorizedException("User role not found");
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        public string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        }

        public IEnumerable<string> GetRoles()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        public bool HasPermission(string permission)
        {
            return _httpContextAccessor.HttpContext?.User?
                .HasClaim("permission", permission) ?? false;
        }
    }
}