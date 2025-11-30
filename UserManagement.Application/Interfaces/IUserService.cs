using UserManagement.Application.DTOs;

namespace UserManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<UserDto> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken cancellationToken = default);
        Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default);
        Task ToggleUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
        Task UpdateUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

        Task<PagedResponse<UserDto>> GetUsersAsync(UserQueryDto query, CancellationToken cancellationToken = default);
        Task<List<UserDto>> GetUsersByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default);
        Task<int> GetUsersCountAsync(CancellationToken cancellationToken = default);
        Task<List<UserDto>> GetInactiveUsersAsync(DateTime olderThan, CancellationToken cancellationToken = default);
        Task BulkUpdateUserStatusAsync(List<Guid> userIds, bool isActive, CancellationToken cancellationToken = default);
        Task CleanupInactiveUsersAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

        Task<string> GenerateRandomPasswordAsync(int length = 12, CancellationToken cancellationToken = default);
        Task<bool> CheckPasswordStrengthAsync(string password, CancellationToken cancellationToken = default);
        Task<int> GetUserProductsCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default);
    }
}