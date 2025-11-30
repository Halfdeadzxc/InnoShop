namespace UserManagement.Application.Interfaces
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
