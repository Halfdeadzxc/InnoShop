using UserManagement.Application.DTOs;

namespace UserManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto> RegisterAsync(RegisterUserDto registerDto, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
        Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
        Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordDto resetDto, CancellationToken cancellationToken = default);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}