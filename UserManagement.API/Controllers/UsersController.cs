using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.DTOs;
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

        public UsersController(
            IUserService userService,
            ICurrentUserService currentUserService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResponse<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsers(
            [FromQuery] UserQueryDto query,
            CancellationToken cancellationToken = default)
        {
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
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            return Ok(user);
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
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
            // Users can only update their own profile, unless they're admin
            if (id != _currentUserService.UserId && !_currentUserService.IsInRole(UserRole.Admin.ToString()))
            {
                return Forbid();
            }

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
            // Users can only delete their own profile, unless they're admin
            if (id != _currentUserService.UserId && !_currentUserService.IsInRole(UserRole.Admin.ToString()))
            {
                return Forbid();
            }

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
            await _userService.ChangePasswordAsync(_currentUserService.GetRequiredUserId(), changePasswordDto, cancellationToken);
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
            await _userService.UpdateUserRoleAsync(id, roleDto.Role, cancellationToken);
            return Ok(new { message = "User role updated successfully" });
        }
    }
}