using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Application.DTOs;
using UserManagement.Application.Exceptions;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Enums;
using EntityFrameworkCore.Testing.Moq;
using Xunit;

namespace UserManagement.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IProductCommunicationService> _productCommunicationServiceMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;
        private readonly CancellationToken _cancellationToken;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _productCommunicationServiceMock = new Mock<IProductCommunicationService>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _cancellationToken = CancellationToken.None;

            _userService = new UserService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _passwordHasherMock.Object,
                _productCommunicationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserExists_ReturnsUserDto()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);

            var result = await _userService.GetUserByIdAsync(userId, _cancellationToken);

            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _userService.GetUserByIdAsync(userId, _cancellationToken));
        }

        [Fact]
        public async Task GetUserByEmailAsync_UserExists_ReturnsUserDto()
        {
            var email = "test@example.com";
            var user = CreateTestUser(email: email);
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, _cancellationToken))
                .ReturnsAsync(user);

            var result = await _userService.GetUserByEmailAsync(email, _cancellationToken);

            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task GetUserByEmailAsync_UserNotFound_ThrowsNotFoundException()
        {
            var email = "nonexistent@example.com";
            _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, _cancellationToken))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _userService.GetUserByEmailAsync(email, _cancellationToken));
        }

        [Fact]
        public async Task UpdateUserAsync_ValidData_UpdatesUser()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var updateDto = new UpdateUserDto
            {
                FirstName = "Updated",
                LastName = "User",
                Email = "updated@example.com"
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(updateDto.Email, _cancellationToken))
                .ReturnsAsync(false);
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, _cancellationToken))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserAsync(userId, updateDto, _cancellationToken);

            Assert.Equal(updateDto.FirstName, result.FirstName);
            Assert.Equal(updateDto.LastName, result.LastName);
            Assert.Equal(updateDto.Email.ToLower(), result.Email);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_EmailAlreadyExists_ThrowsConflictException()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var updateDto = new UpdateUserDto { Email = "existing@example.com" };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.ExistsByEmailAsync(updateDto.Email, _cancellationToken))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() =>
                _userService.UpdateUserAsync(userId, updateDto, _cancellationToken));
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_DeletesUser()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.DeleteAsync(user, _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.DeleteUserAsync(userId, _cancellationToken);

            _userRepositoryMock.Verify(x => x.DeleteAsync(user, _cancellationToken), Times.Once);
            _productCommunicationServiceMock.Verify(x =>
                x.ToggleUserProductsAsync(userId, false, default), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidData_ChangesPassword()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "oldPassword",
                NewPassword = "newStrongPassword123!"
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                .Returns(true);
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(changePasswordDto.NewPassword))
                .Returns(true);
            _passwordHasherMock.Setup(x => x.HashPassword(changePasswordDto.NewPassword))
                .Returns("hashedNewPassword");
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.ChangePasswordAsync(userId, changePasswordDto, _cancellationToken);

            Assert.Equal("hashedNewPassword", user.PasswordHash);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ThrowsValidationException()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "wrongPassword",
                NewPassword = "newPassword123!"
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
                .Returns(false);

            await Assert.ThrowsAsync<ValidationException>(() =>
                _userService.ChangePasswordAsync(userId, changePasswordDto, _cancellationToken));
        }

        [Fact]
        public async Task ToggleUserStatusAsync_ValidUser_TogglesStatus()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var newStatus = false;

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.ToggleUserStatusAsync(userId, newStatus, _cancellationToken);

            Assert.Equal(newStatus, user.IsActive);
            _productCommunicationServiceMock.Verify(x =>
                x.ToggleUserProductsAsync(userId, newStatus, default), Times.Once);
        }

        [Fact]
        public async Task UpdateUserRoleAsync_ValidRole_UpdatesRole()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var newRole = "Admin";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(x => x.UpdateAsync(user, _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.UpdateUserRoleAsync(userId, newRole, _cancellationToken);

            Assert.Equal(UserRole.Admin, user.Role);
            _userRepositoryMock.Verify(x => x.UpdateAsync(user, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task UpdateUserRoleAsync_InvalidRole_ThrowsValidationException()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var invalidRole = "InvalidRole";

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, _cancellationToken))
                .ReturnsAsync(user);

            await Assert.ThrowsAsync<ValidationException>(() =>
                _userService.UpdateUserRoleAsync(userId, invalidRole, _cancellationToken));
        }

        [Fact]
        public async Task BulkUpdateUserStatusAsync_ValidUserIds_UpdatesAllUsers()
        {
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var users = userIds.Select(id => CreateTestUser(id)).ToList();
            var newStatus = false;

            _userRepositoryMock.Setup(x => x.GetUsersByIdsAsync(userIds, _cancellationToken))
                .ReturnsAsync(users);
            _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>(), _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.BulkUpdateUserStatusAsync(userIds, newStatus, _cancellationToken);

            foreach (var user in users)
            {
                Assert.Equal(newStatus, user.IsActive);
            }
            _userRepositoryMock.Verify(x =>
                x.UpdateAsync(It.IsAny<User>(), _cancellationToken),
                Times.Exactly(users.Count));
        }

        [Fact]
        public async Task CleanupInactiveUsersAsync_ValidCutoffDate_DeletesInactiveUsers()
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-6);
            var inactiveUsers = new List<User>
            {
                CreateTestUser(isActive: false),
                CreateTestUser(isActive: false)
            };

            _userRepositoryMock.Setup(x => x.GetInactiveUsersAsync(cutoffDate, _cancellationToken))
                .ReturnsAsync(inactiveUsers);
            _userRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<User>(), _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.CleanupInactiveUsersAsync(cutoffDate, _cancellationToken);

            _userRepositoryMock.Verify(x =>
                x.DeleteAsync(It.IsAny<User>(), _cancellationToken),
                Times.Exactly(inactiveUsers.Count));
        }

        [Fact]
        public async Task GetUsersByIdsAsync_ValidIds_ReturnsUsers()
        {
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var users = userIds.Select(id => CreateTestUser(id)).ToList();

            _userRepositoryMock.Setup(x => x.GetUsersByIdsAsync(userIds, _cancellationToken))
                .ReturnsAsync(users);

            var result = await _userService.GetUsersByIdsAsync(userIds, _cancellationToken);

            Assert.NotNull(result);
            Assert.Equal(users.Count, result.Count);
        }

        [Fact]
        public async Task GetUsersCountAsync_ReturnsCount()
        {
            var expectedCount = 5;
            _userRepositoryMock.Setup(x => x.GetTotalCountAsync(_cancellationToken))
                .ReturnsAsync(expectedCount);

            var result = await _userService.GetUsersCountAsync(_cancellationToken);

            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task GetInactiveUsersAsync_ReturnsInactiveUsers()
        {
            var olderThan = DateTime.UtcNow.AddMonths(-6);
            var inactiveUsers = new List<User>
            {
                CreateTestUser(isActive: false),
                CreateTestUser(isActive: false)
            };

            _userRepositoryMock.Setup(x => x.GetInactiveUsersAsync(olderThan, _cancellationToken))
                .ReturnsAsync(inactiveUsers);

            var result = await _userService.GetInactiveUsersAsync(olderThan, _cancellationToken);

            Assert.NotNull(result);
            Assert.Equal(inactiveUsers.Count, result.Count);
            Assert.All(result, u => Assert.False(u.IsActive));
        }

        [Fact]
        public async Task GenerateRandomPasswordAsync_ReturnsPassword()
        {
            var length = 12;
            var expectedPassword = "randomPassword123";
            _passwordHasherMock.Setup(x => x.GenerateRandomPassword(length))
                .Returns(expectedPassword);

            var result = await _userService.GenerateRandomPasswordAsync(length, _cancellationToken);

            Assert.Equal(expectedPassword, result);
        }

        [Fact]
        public async Task CheckPasswordStrengthAsync_StrongPassword_ReturnsTrue()
        {
            var password = "StrongPassword123!";
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(password))
                .Returns(true);

            var result = await _userService.CheckPasswordStrengthAsync(password, _cancellationToken);

            Assert.True(result);
        }

        [Fact]
        public async Task CheckPasswordStrengthAsync_WeakPassword_ReturnsFalse()
        {
            var password = "weak";
            _passwordHasherMock.Setup(x => x.IsPasswordStrong(password))
                .Returns(false);

            var result = await _userService.CheckPasswordStrengthAsync(password, _cancellationToken);

            Assert.False(result);
        }

        [Fact]
        public async Task GetUserProductsCountAsync_ValidUser_ReturnsCount()
        {
            var userId = Guid.NewGuid();
            var expectedCount = 5;
            _productCommunicationServiceMock.Setup(x => x.GetUserProductsCountAsync(userId, _cancellationToken))
                .ReturnsAsync(expectedCount);

            var result = await _userService.GetUserProductsCountAsync(userId, _cancellationToken);

            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_ValidData_SendsEmail()
        {
            var email = "test@example.com";
            var firstName = "Test";
            _emailServiceMock.Setup(x => x.SendWelcomeEmailAsync(email, firstName, _cancellationToken))
                .Returns(Task.CompletedTask);

            await _userService.SendWelcomeEmailAsync(email, firstName, _cancellationToken);

            _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(email, firstName, _cancellationToken), Times.Once);
        }

        private User CreateTestUser(Guid? id = null, string email = "test@example.com", bool isActive = true)
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = email,
                PasswordHash = "hashedPassword",
                Role = UserRole.User,
                IsActive = isActive,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}