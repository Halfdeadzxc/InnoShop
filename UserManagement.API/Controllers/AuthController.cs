using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;

namespace UserManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Register(
            [FromBody] RegisterUserDto registerDto,
            CancellationToken cancellationToken = default)
        {
            var user = await _authService.RegisterAsync(registerDto, cancellationToken);
            return Ok(user);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login(
            [FromBody] LoginDto loginDto,
            CancellationToken cancellationToken = default)
        {
            var authResponse = await _authService.LoginAsync(loginDto, cancellationToken);
            return Ok(authResponse);
        }

        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string token,
            CancellationToken cancellationToken = default)
        {
            await _authService.ConfirmEmailAsync(token, cancellationToken);
            return Ok(new { message = "Email confirmed successfully" });
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto forgotPasswordDto,
            CancellationToken cancellationToken = default)
        {
            await _authService.ForgotPasswordAsync(forgotPasswordDto.Email, cancellationToken);
            return Ok(new { message = "Password reset email sent" });
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDto resetDto,
            CancellationToken cancellationToken = default)
        {
            await _authService.ResetPasswordAsync(resetDto, cancellationToken);
            return Ok(new { message = "Password reset successfully" });
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> RefreshToken(
            [FromBody] RefreshTokenDto refreshDto,
            CancellationToken cancellationToken = default)
        {
            var authResponse = await _authService.RefreshTokenAsync(refreshDto.RefreshToken, cancellationToken);
            return Ok(authResponse);
        }
    }
}