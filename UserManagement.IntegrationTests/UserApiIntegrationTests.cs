using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.DTOs;
using UserManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;
using UserManagement.API;

namespace UserManagement.IntegrationTests
{
    public class UserApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly UserDbContext _dbContext;

        public UserApiIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            var scope = _factory.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

            // Clean database before each test
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                FirstName = "Integration",
                LastName = "Test",
                Email = "integration@test.com",
                Password = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().NotBeNull();
            user.Email.Should().Be(registerDto.Email.ToLower());
            user.FirstName.Should().Be(registerDto.FirstName);
            user.LastName.Should().Be(registerDto.LastName);
            user.IsActive.Should().BeTrue();
            user.EmailConfirmed.Should().BeFalse(); // Email not confirmed initially
        }

        [Fact]
        public async Task Register_WithExistingEmail_ShouldReturnConflict()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                FirstName = "Duplicate",
                LastName = "User",
                Email = "duplicate@test.com",
                Password = "Password123!"
            };

            // First registration
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Second registration with same email
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidRegisterDto = new RegisterUserDto
            {
                FirstName = "", // Invalid - empty
                LastName = "Test",
                Email = "invalid-email", // Invalid email
                Password = "weak" // Weak password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", invalidRegisterDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                FirstName = "Login",
                LastName = "Test",
                Email = "login@test.com",
                Password = "Password123!"
            };

            // Register user first
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var createdUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();

            // Confirm email (in real scenario this would be done via email confirmation)
            var user = await _dbContext.Users.FindAsync(createdUser.Id);
            user.EmailConfirmed = true;
            await _dbContext.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "login@test.com",
                Password = "Password123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authResponse.Should().NotBeNull();
            authResponse.AccessToken.Should().NotBeNullOrEmpty();
            authResponse.RefreshToken.Should().NotBeNullOrEmpty();
            authResponse.User.Should().NotBeNull();
            authResponse.User.Email.Should().Be(loginDto.Email);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "WrongPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUser_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _client.GetAsync($"/api/users/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().NotBeNull();
            user.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var (_, token) = await RegisterAndLoginUser();
            var invalidUserId = Guid.NewGuid();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _client.GetAsync($"/api/users/{invalidUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUser_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/users/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentUser_WithValidToken_ShouldReturnUser()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _client.GetAsync("/api/users/me");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().NotBeNull();
            user.Id.Should().Be(userId);
        }

        [Fact]
        public async Task UpdateUser_WithValidData_ShouldReturnUpdatedUser()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var updateDto = new UpdateUserDto
            {
                FirstName = "Updated",
                LastName = "User",
                Email = "updated@test.com"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/users/{userId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
            updatedUser.Should().NotBeNull();
            updatedUser.Id.Should().Be(userId);
            updatedUser.FirstName.Should().Be(updateDto.FirstName);
            updatedUser.LastName.Should().Be(updateDto.LastName);
            updatedUser.Email.Should().Be(updateDto.Email.ToLower());
        }

        [Fact]
        public async Task UpdateUser_OtherUserWithoutAdmin_ShouldReturnForbidden()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();
            var otherUserId = Guid.NewGuid();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var updateDto = new UpdateUserDto
            {
                FirstName = "Updated"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/users/{otherUserId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/users/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidCurrentPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var (_, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/users/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUsers_AsAdmin_ShouldReturnUsers()
        {
            // Arrange - Create admin user
            var (adminId, adminToken) = await RegisterAndLoginAdmin();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            // Create some test users
            await RegisterTestUser("user1@test.com");
            await RegisterTestUser("user2@test.com");

            // Act
            var response = await _client.GetAsync("/api/users?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var users = await response.Content.ReadFromJsonAsync<PagedResponse<UserDto>>();
            users.Should().NotBeNull();
            users.Items.Should().NotBeEmpty();
            users.TotalCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetUsers_AsRegularUser_ShouldReturnForbidden()
        {
            // Arrange
            var (_, userToken) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userToken}");

            // Act
            var response = await _client.GetAsync("/api/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ToggleUserStatus_AsAdmin_ShouldReturnSuccess()
        {
            // Arrange
            var (adminId, adminToken) = await RegisterAndLoginAdmin();
            var regularUser = await RegisterTestUser("status@test.com");

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var statusDto = new ToggleUserStatusDto { IsActive = false };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/users/{regularUser.Id}/status", statusDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UpdateUserRole_AsAdmin_ShouldReturnSuccess()
        {
            // Arrange
            var (adminId, adminToken) = await RegisterAndLoginAdmin();
            var regularUser = await RegisterTestUser("role@test.com");

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

            var roleDto = new UpdateUserRoleDto { Role = "Admin" };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/users/{regularUser.Id}/role", roleDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeleteUser_OwnAccount_ShouldReturnNoContent()
        {
            // Arrange
            var (userId, token) = await RegisterAndLoginUser();

            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            // Act
            var response = await _client.DeleteAsync($"/api/users/{userId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private async Task<(Guid userId, string token)> RegisterAndLoginUser()
        {
            var registerDto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = $"{Guid.NewGuid()}@test.com",
                Password = "Password123!"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var createdUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();

            // Confirm email
            var user = await _dbContext.Users.FindAsync(createdUser.Id);
            user.EmailConfirmed = true;
            await _dbContext.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = registerDto.Email,
                Password = registerDto.Password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

            return (createdUser.Id, authResponse.AccessToken);
        }

        private async Task<(Guid userId, string token)> RegisterAndLoginAdmin()
        {
            var registerDto = new RegisterUserDto
            {
                FirstName = "Admin",
                LastName = "User",
                Email = $"{Guid.NewGuid()}@admin.com",
                Password = "Password123!"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var createdUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();

            // Set as admin and confirm email
            var user = await _dbContext.Users.FindAsync(createdUser.Id);
            user.Role = Domain.Enums.UserRole.Admin;
            user.EmailConfirmed = true;
            await _dbContext.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = registerDto.Email,
                Password = registerDto.Password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

            return (createdUser.Id, authResponse.AccessToken);
        }

        private async Task<UserDto> RegisterTestUser(string email)
        {
            var registerDto = new RegisterUserDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = email,
                Password = "Password123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            _client?.Dispose();
        }
    }

    // DTO classes for testing
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class ToggleUserStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateUserRoleDto
    {
        public string Role { get; set; }
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}