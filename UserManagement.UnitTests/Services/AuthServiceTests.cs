using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using Xunit;

namespace UserManagement.Application.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _emailServiceMock = new Mock<IEmailService>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _userRepositoryMock.Object,
                _tokenServiceMock.Object,
                _emailServiceMock.Object,
                _passwordHasherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_RegistersUser()
        {
            var registerDto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "StrongPassword123!"
            };

            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(registerDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(registerDto.Password))
                .Returns(true);
            _passwordHasherMock.Setup(x => x.HashPassword(registerDto.Password))
                .Returns("hashedPassword");
            _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _authService.RegisterAsync(registerDto);

            Assert.NotNull(result);
            Assert.Equal(registerDto.Email, result.Email);
            Assert.Equal(registerDto.FirstName, result.FirstName);
            Assert.Equal(registerDto.LastName, result.LastName);
            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendConfirmationEmailAsync(registerDto.Email, It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyExists_ThrowsConflictException()
        {
            var registerDto = new RegisterUserDto
            {
                Email = "existing@example.com",
                Password = "Password123!"
            };

            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(registerDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _authService.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task RegisterAsync_WeakPassword_ThrowsValidationException()
        {
            var registerDto = new RegisterUserDto
            {
                Email = "test@example.com",
                Password = "weak"
            };

            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(registerDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(registerDto.Password))
                .Returns(false);

            await Assert.ThrowsAsync<ValidationException>(() =>
                _authService.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
        {
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "password123"
            };

            var user = CreateTestUser(loginDto.Email);
            var accessToken = "access-token";
            var refreshToken = "refresh-token";

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
                .Returns(accessToken);
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            var result = await _authService.LoginAsync(loginDto);

            Assert.NotNull(result);
            Assert.Equal(accessToken, result.AccessToken);
            Assert.Equal(refreshToken, result.RefreshToken);
            Assert.NotNull(result.User);
            Assert.Equal(user.Email, result.User.Email);
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedException()
        {
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "password123"
            };

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
        {
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            var user = CreateTestUser(loginDto.Email);

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(false);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_InactiveUser_ThrowsBusinessRuleException()
        {
            var loginDto = new LoginDto
            {
                Email = "inactive@example.com",
                Password = "password123"
            };

            var user = CreateTestUser(loginDto.Email, isActive: false);

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);

            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_EmailNotConfirmed_ThrowsEmailNotConfirmedException()
        {
            var loginDto = new LoginDto
            {
                Email = "unconfirmed@example.com",
                Password = "password123"
            };

            var user = CreateTestUser(loginDto.Email, emailConfirmed: false);

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);

            await Assert.ThrowsAsync<EmailNotConfirmedException>(() =>
                _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task ConfirmEmailAsync_ValidToken_ConfirmsEmail()
        {
            var token = "valid-token";
            var user = CreateTestUser(emailConfirmed: false);
            user.EmailConfirmationToken = token;

            _userRepositoryMock.Setup(x => x.GetByEmailConfirmationTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _authService.ConfirmEmailAsync(token);

            Assert.True(user.EmailConfirmed);
            Assert.Null(user.EmailConfirmationToken);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(user.Email, user.FirstName, default), Times.Once);
        }

        [Fact]
        public async Task ConfirmEmailAsync_InvalidToken_ThrowsInvalidTokenException()
        {
            var token = "invalid-token";
            _userRepositoryMock.Setup(x => x.GetByEmailConfirmationTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<InvalidTokenException>(() =>
                _authService.ConfirmEmailAsync(token));
        }

        [Fact]
        public async Task ForgotPasswordAsync_ValidEmail_GeneratesResetToken()
        {
            var email = "test@example.com";
            var user = CreateTestUser(email);

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _authService.ForgotPasswordAsync(email);

            Assert.NotNull(user.PasswordResetToken);
            Assert.NotNull(user.ResetTokenExpires);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(email, It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_NonExistentEmail_DoesNothing()
        {
            var email = "nonexistent@example.com";
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            await _authService.ForgotPasswordAsync(email);

            _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ResetsPassword()
        {
            var resetDto = new ResetPasswordDto
            {
                Token = "valid-token",
                NewPassword = "NewStrongPassword123!"
            };

            var user = CreateTestUser();
            user.PasswordResetToken = resetDto.Token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            _userRepositoryMock.Setup(x => x.GetByPasswordResetTokenAsync(resetDto.Token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(resetDto.NewPassword))
                .Returns(true);
            _passwordHasherMock.Setup(x => x.HashPassword(resetDto.NewPassword))
                .Returns("hashedNewPassword");
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _authService.ResetPasswordAsync(resetDto);

            Assert.Equal("hashedNewPassword", user.PasswordHash);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.ResetTokenExpires);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_ThrowsInvalidTokenException()
        {
            var resetDto = new ResetPasswordDto
            {
                Token = "invalid-token",
                NewPassword = "NewPassword123!"
            };

            _userRepositoryMock.Setup(x => x.GetByPasswordResetTokenAsync(resetDto.Token, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<InvalidTokenException>(() =>
                _authService.ResetPasswordAsync(resetDto));
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ThrowsInvalidTokenException()
        {
            var resetDto = new ResetPasswordDto
            {
                Token = "expired-token",
                NewPassword = "NewPassword123!"
            };

            var user = CreateTestUser();
            user.PasswordResetToken = resetDto.Token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(-1);

            _userRepositoryMock.Setup(x => x.GetByPasswordResetTokenAsync(resetDto.Token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<InvalidTokenException>(() =>
                _authService.ResetPasswordAsync(resetDto));
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
        {
            var refreshToken = "valid-refresh-token";
            var userId = Guid.NewGuid();
            var user = CreateTestUser();
            var newAccessToken = "new-access-token";
            var newRefreshToken = "new-refresh-token";

            _tokenServiceMock.Setup(x => x.GetUserIdFromToken(refreshToken))
                .Returns(userId);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(user))
                .Returns(newAccessToken);
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            var result = await _authService.RefreshTokenAsync(refreshToken);

            Assert.NotNull(result);
            Assert.Equal(newAccessToken, result.AccessToken);
            Assert.Equal(newRefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorizedException()
        {
            var refreshToken = "invalid-refresh-token";
            _tokenServiceMock.Setup(x => x.GetUserIdFromToken(refreshToken))
                .Returns((Guid?)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _authService.RefreshTokenAsync(refreshToken));
        }

        [Fact]
        public async Task RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            var refreshToken = "valid-refresh-token";
            var userId = Guid.NewGuid();

            _tokenServiceMock.Setup(x => x.GetUserIdFromToken(refreshToken))
                .Returns(userId);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                _authService.RefreshTokenAsync(refreshToken));
        }

        private User CreateTestUser(string email = "test@example.com", bool isActive = true, bool emailConfirmed = true)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PasswordHash = "hashedPassword",
                Role = UserRole.User,
                IsActive = isActive,
                EmailConfirmed = emailConfirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}