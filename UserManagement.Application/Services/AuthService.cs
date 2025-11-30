using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using UserManagement.Application.Interfaces;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace UserManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailService emailService,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto registerDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registering new user with email: {Email}", registerDto.Email);

            if (await _userRepository.ExistsByEmailAsync(registerDto.Email, cancellationToken))
            {
                throw new ConflictException("Email is already registered");
            }

            if (!IsValidEmail(registerDto.Email))
            {
                throw new ValidationException("Invalid email format");
            }

            if (!_passwordHasher.IsPasswordStrong(registerDto.Password))
            {
                throw new ValidationException("Password does not meet security requirements");
            }

            var user = new User
            {
                FirstName = registerDto.FirstName.Trim(),
                LastName = registerDto.LastName.Trim(),
                Email = registerDto.Email.ToLower().Trim(),
                PasswordHash = _passwordHasher.HashPassword(registerDto.Password),
                EmailConfirmationToken = GenerateSecureToken(),
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendConfirmationEmailAsync(user.Email, user.EmailConfirmationToken);
                    _logger.LogInformation("Confirmation email sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                }
            });

            _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);

            return MapToDto(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var user = await _userRepository.GetByEmailAsync(loginDto.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found for email: {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!_passwordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - invalid password for email: {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - account deactivated for email: {Email}", loginDto.Email);
                throw new BusinessRuleException("Account is deactivated. Please contact administrator.");
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Login failed - email not confirmed for email: {Email}", loginDto.Email);
                throw new EmailNotConfirmedException();
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var userDto = MapToDto(user);

            _logger.LogInformation("Login successful for user ID: {UserId}", user.Id);

            return new AuthResponse(accessToken, refreshToken, userDto);
        }

        public async Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Confirming email with token");

            var user = await _userRepository.GetByEmailConfirmationTokenAsync(token, cancellationToken);
            if (user == null)
            {
                throw new InvalidTokenException();
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Send welcome email in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                    _logger.LogInformation("Welcome email sent to: {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }
            });

            _logger.LogInformation("Email confirmed successfully for user ID: {UserId}", user.Id);
        }

        public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing forgot password for email: {Email}", email);

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogInformation("Forgot password request for non-existent email: {Email}", email);
                return;
            }

            if (!user.IsActive)
            {
                throw new BusinessRuleException("Account is deactivated");
            }

            user.PasswordResetToken = GenerateSecureToken();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(24);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Send password reset email in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken);
                    _logger.LogInformation("Password reset email sent to: {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                }
            });

            _logger.LogInformation("Password reset token generated for user ID: {UserId}", user.Id);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto resetDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resetting password with token");

            var user = await _userRepository.GetByPasswordResetTokenAsync(resetDto.Token, cancellationToken);
            if (user == null || user.ResetTokenExpires < DateTime.UtcNow)
            {
                throw new InvalidTokenException();
            }

            if (!_passwordHasher.IsPasswordStrong(resetDto.NewPassword))
            {
                throw new ValidationException("New password does not meet security requirements");
            }

            user.PasswordHash = _passwordHasher.HashPassword(resetDto.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Password reset successfully for user ID: {UserId}", user.Id);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing token");

            // Validate refresh token
            var userId = _tokenService.GetUserIdFromToken(refreshToken);
            if (userId == null)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user == null || !user.IsActive || !user.EmailConfirmed)
            {
                throw new UnauthorizedException("User not found or inactive");
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var userDto = MapToDto(user);

            _logger.LogInformation("Token refreshed successfully for user ID: {UserId}", user.Id);

            return new AuthResponse(newAccessToken, newRefreshToken, userDto);
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