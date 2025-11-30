using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<User?> GetByEmailConfirmationTokenAsync(string token, CancellationToken cancellationToken = default);

        Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);

        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        Task DeleteAsync(User user, CancellationToken cancellationToken = default);

        IQueryable<User> GetQueryable();

        Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default);

        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        Task<List<User>> GetInactiveUsersAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}
