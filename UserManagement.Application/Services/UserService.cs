using Microsoft.EntityFrameworkCore;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using UserManagement.Application.Interfaces;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace UserManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IProductCommunicationService _productCommunicationService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IEmailService emailService,
            IPasswordHasher passwordHasher,
            IProductCommunicationService productCommunicationService,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _productCommunicationService = productCommunicationService;
            _logger = logger;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting user by ID: {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", id);
            }

            return MapToDto(user);
        }

        public async Task<UserDto> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting user by email: {Email}", email);

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", email);
            }

            return MapToDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating user ID: {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", id);
            }

            bool emailChanged = false;

            if (!string.IsNullOrWhiteSpace(updateDto.FirstName))
            {
                user.FirstName = updateDto.FirstName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(updateDto.LastName))
            {
                user.LastName = updateDto.LastName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != user.Email)
            {
                if (!IsValidEmail(updateDto.Email))
                {
                    throw new ValidationException("Invalid email format");
                }

                if (await _userRepository.ExistsByEmailAsync(updateDto.Email, cancellationToken))
                {
                    throw new ConflictException("Email is already registered");
                }

                user.Email = updateDto.Email.ToLower().Trim();
                user.EmailConfirmed = false;
                user.EmailConfirmationToken = GenerateSecureToken();
                emailChanged = true;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

            if (emailChanged)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendConfirmationEmailAsync(user.Email, user.EmailConfirmationToken!);
                        _logger.LogInformation("Confirmation email sent to new email: {Email}", user.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                    }
                });
            }

            _logger.LogInformation("User updated successfully: {UserId}", user.Id);

            return MapToDto(user);
        }

        public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting user ID: {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", id);
            }

            await _userRepository.DeleteAsync(user, cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _productCommunicationService.ToggleUserProductsAsync(id, false);
                    _logger.LogInformation("Products deactivated for deleted user: {UserId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deactivate products for deleted user: {UserId}", id);
                }
            });

            _logger.LogInformation("User deleted successfully: {UserId}", id);
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Changing password for user ID: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            if (!_passwordHasher.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                throw new ValidationException("Current password is incorrect");
            }

            if (!_passwordHasher.IsPasswordStrong(changePasswordDto.NewPassword))
            {
                throw new ValidationException("New password does not meet security requirements");
            }

            user.PasswordHash = _passwordHasher.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
        }

        public async Task ToggleUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Toggling user status for ID: {UserId} to {IsActive}", userId, isActive);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _productCommunicationService.ToggleUserProductsAsync(userId, isActive);
                    _logger.LogInformation("Products toggled successfully for user ID: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to toggle products for user ID: {UserId}", userId);
                }
            });

            _logger.LogInformation("User status toggled successfully for ID: {UserId} to {IsActive}", userId, isActive);
        }

        public async Task UpdateUserRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating role for user ID: {UserId} to {Role}", userId, role);

            if (!Enum.TryParse<UserRole>(role, out var userRole))
            {
                throw new ValidationException("Invalid role");
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            user.Role = userRole;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Role updated successfully for user ID: {UserId} to {Role}", userId, role);
        }

        public async Task<PagedResponse<UserDto>> GetUsersAsync(UserQueryDto query, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting users with query: {@Query}", query);

            var usersQuery = _userRepository.GetQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, out var role))
            {
                usersQuery = usersQuery.Where(u => u.Role == role);
            }

            if (query.IsActive.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
            }

            var totalCount = await usersQuery.CountAsync(cancellationToken);

            usersQuery = query.SortBy.ToLower() switch
            {
                "email" => query.SortDescending ? usersQuery.OrderByDescending(u => u.Email) : usersQuery.OrderBy(u => u.Email),
                "firstname" => query.SortDescending ? usersQuery.OrderByDescending(u => u.FirstName) : usersQuery.OrderBy(u => u.FirstName),
                "lastname" => query.SortDescending ? usersQuery.OrderByDescending(u => u.LastName) : usersQuery.OrderBy(u => u.LastName),
                _ => query.SortDescending ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt)
            };

            var users = await usersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var userDtos = users.Select(MapToDto).ToList();

            _logger.LogDebug("Retrieved {Count} users out of {TotalCount}", userDtos.Count, totalCount);

            return new PagedResponse<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<List<UserDto>> GetUsersByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting users by IDs: {UserIds}", userIds);

            var users = await _userRepository.GetUsersByIdsAsync(userIds, cancellationToken);
            return users.Select(MapToDto).ToList();
        }

        public async Task<int> GetUsersCountAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting total users count");

            return await _userRepository.GetTotalCountAsync(cancellationToken);
        }

        public async Task<List<UserDto>> GetInactiveUsersAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting inactive users older than: {OlderThan}", olderThan);

            var users = await _userRepository.GetInactiveUsersAsync(olderThan, cancellationToken);
            return users.Select(MapToDto).ToList();
        }

        public async Task BulkUpdateUserStatusAsync(List<Guid> userIds, bool isActive, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bulk updating status for {Count} users to {IsActive}", userIds.Count, isActive);

            var users = await _userRepository.GetUsersByIdsAsync(userIds, cancellationToken);

            foreach (var user in users)
            {
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var userId in userIds)
                    {
                        await _productCommunicationService.ToggleUserProductsAsync(userId, isActive);
                    }
                    _logger.LogInformation("Bulk products toggle completed for {Count} users", userIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to bulk toggle products for users");
                }
            });

            _logger.LogInformation("Bulk user status update completed for {Count} users", users.Count);
        }

        public async Task CleanupInactiveUsersAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cleaning up inactive users older than: {CutoffDate}", cutoffDate);

            var inactiveUsers = await _userRepository.GetInactiveUsersAsync(cutoffDate, cancellationToken);

            foreach (var user in inactiveUsers)
            {
                await _userRepository.DeleteAsync(user, cancellationToken);
                _logger.LogDebug("Deleted inactive user: {UserId} ({Email})", user.Id, user.Email);
            }

            _logger.LogInformation("Cleaned up {Count} inactive users", inactiveUsers.Count);
        }

        public async Task<string> GenerateRandomPasswordAsync(int length = 12, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Generating random password with length: {Length}", length);

            return await Task.Run(() => _passwordHasher.GenerateRandomPassword(length));
        }

        public async Task<bool> CheckPasswordStrengthAsync(string password, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking password strength");

            return await Task.Run(() => _passwordHasher.IsPasswordStrong(password));
        }

        public async Task<int> GetUserProductsCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products count for user ID: {UserId}", userId);

            return await _productCommunicationService.GetUserProductsCountAsync(userId, cancellationToken);
        }

        public async Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending welcome email to: {Email}", email);

            await _emailService.SendWelcomeEmailAsync(email, firstName, cancellationToken);

            _logger.LogInformation("Welcome email sent successfully to: {Email}", email);
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt
        };

        private static string GenerateSecureToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "").Replace("+", "").Replace("=", "");
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}