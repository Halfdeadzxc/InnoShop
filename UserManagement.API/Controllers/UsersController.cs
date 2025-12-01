using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Interfaces;
using UserManagement.Domain.Enums;

namespace UserManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResponse<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
            [FromQuery] UserQueryDto query,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting users with query: {@Query}", query);
            var users = await _userService.GetUsersAsync(query, cancellationToken);
            return Ok(users);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting user by ID: {UserId}", id);
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            return Ok(user);
        }

        [HttpGet("by-email/{email}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserByEmail(
            string email,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting user by email: {Email}", email);
            var user = await _userService.GetUserByEmailAsync(email, cancellationToken);
            return Ok(user);
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogDebug("Getting current user: {UserId}", userId);
            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            return Ok(user);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> UpdateUser(
            Guid id,
            [FromBody] UpdateUserDto updateDto,
            CancellationToken cancellationToken = default)
        {
            if (id != _currentUserService.UserId && !_currentUserService.IsInRole(UserRole.Admin.ToString()))
            {
                _logger.LogWarning("User {CurrentUserId} attempted to update user {TargetUserId} without permission",
                    _currentUserService.UserId, id);
                return Forbid();
            }

            _logger.LogInformation("Updating user ID: {UserId}", id);
            var user = await _userService.UpdateUserAsync(id, updateDto, cancellationToken);
            return Ok(user);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (id != _currentUserService.UserId && !_currentUserService.IsInRole(UserRole.Admin.ToString()))
            {
                _logger.LogWarning("User {CurrentUserId} attempted to delete user {TargetUserId} without permission",
                    _currentUserService.UserId, id);
                return Forbid();
            }

            _logger.LogInformation("Deleting user ID: {UserId}", id);
            await _userService.DeleteUserAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto changePasswordDto,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Changing password for user ID: {UserId}", userId);
            await _userService.ChangePasswordAsync(userId, changePasswordDto, cancellationToken);
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleUserStatus(
            Guid id,
            [FromBody] ToggleUserStatusDto statusDto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Toggling user status for ID: {UserId} to {IsActive}", id, statusDto.IsActive);
            await _userService.ToggleUserStatusAsync(id, statusDto.IsActive, cancellationToken);
            return Ok(new { message = $"User {(statusDto.IsActive ? "activated" : "deactivated")} successfully" });
        }

        [HttpPatch("{id:guid}/role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRole(
            Guid id,
            [FromBody] UpdateUserRoleDto roleDto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating role for user ID: {UserId} to {Role}", id, roleDto.Role);
            await _userService.UpdateUserRoleAsync(id, roleDto.Role, cancellationToken);
            return Ok(new { message = "User role updated successfully" });
        }

        [HttpGet("{id:guid}/exists")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> UserExists(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if user exists: {UserId}", id);
            try
            {
                var user = await _userService.GetUserByIdAsync(id, cancellationToken);
                return Ok(true);
            }
            catch (NotFoundException)
            {
                return Ok(false);
            }
        }

        [HttpGet("{id:guid}/active")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<bool>> IsUserActive(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking if user is active: {UserId}", id);
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            return Ok(user.IsActive);
        }

        [HttpGet("{id:guid}/name")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<string>> GetUserName(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting user name: {UserId}", id);
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            var fullName = $"{user.FirstName} {user.LastName}";
            return Ok(fullName);
        }

        [HttpGet("count")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> GetUsersCount(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting total users count");
            var count = await _userService.GetUsersCountAsync(cancellationToken);
            return Ok(count);
        }

        [HttpGet("inactive")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDto>>> GetInactiveUsers(
            [FromQuery] DateTime? olderThan = null,
            CancellationToken cancellationToken = default)
        {
            var cutoffDate = olderThan ?? DateTime.UtcNow.AddMonths(-6);
            _logger.LogDebug("Getting inactive users older than: {OlderThan}", cutoffDate);
            var users = await _userService.GetInactiveUsersAsync(cutoffDate, cancellationToken);
            return Ok(users);
        }

        [HttpPost("bulk/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpdateUserStatus(
            [FromBody] BulkUpdateUserStatusDto bulkUpdateDto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bulk updating status for {Count} users to {IsActive}",
                bulkUpdateDto.UserIds.Count, bulkUpdateDto.IsActive);
            await _userService.BulkUpdateUserStatusAsync(bulkUpdateDto.UserIds, bulkUpdateDto.IsActive, cancellationToken);
            return Ok(new { message = "Bulk status update completed successfully" });
        }

        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CleanupInactiveUsers(
            [FromQuery] DateTime? cutoffDate = null,
            CancellationToken cancellationToken = default)
        {
            var date = cutoffDate ?? DateTime.UtcNow.AddMonths(-6);
            _logger.LogInformation("Cleaning up inactive users older than: {CutoffDate}", date);
            await _userService.CleanupInactiveUsersAsync(date, cancellationToken);
            return Ok(new { message = "Inactive users cleanup completed" });
        }

        [HttpGet("password/strength")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> CheckPasswordStrength(
            [FromQuery] string password,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Checking password strength");
            var isStrong = await _userService.CheckPasswordStrengthAsync(password, cancellationToken);
            return Ok(isStrong);
        }

        [HttpGet("password/generate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<ActionResult<string>> GenerateRandomPassword(
            [FromQuery] int length = 12,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Generating random password with length: {Length}", length);
            var password = await _userService.GenerateRandomPasswordAsync(length, cancellationToken);
            return Ok(password);
        }

        [HttpGet("{id:guid}/products/count")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<int>> GetUserProductsCount(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products count for user ID: {UserId}", id);
            var count = await _userService.GetUserProductsCountAsync(id, cancellationToken);
            return Ok(count);
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDto>>> GetUsersByIds(
            [FromBody] List<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting users by IDs: {UserIds}", userIds);
            var users = await _userService.GetUsersByIdsAsync(userIds, cancellationToken);
            return Ok(users);
        }
    }

}