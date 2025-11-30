using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductManagement.Application.Exceptions;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.Infrastructure.Services
{
    public class UserCommunicationService : IUserCommunicationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserCommunicationService> _logger;
        private readonly UserServiceSettings _settings;

        public UserCommunicationService(
            HttpClient httpClient,
            IOptions<UserServiceSettings> settings,
            ILogger<UserCommunicationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> ValidateUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Validating user existence: {UserId}", userId);

                var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/api/users/{userId}/exists", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonSerializer.Deserialize<bool>(content);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }

                _logger.LogWarning("Unexpected response when validating user {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user existence: {UserId}", userId);
                throw new ProductServiceCommunicationException("UserService", "ValidateUserExists", ex);
            }
        }

        public async Task<bool> ValidateUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Validating user active status: {UserId}", userId);

                var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/api/users/{userId}/active", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonSerializer.Deserialize<bool>(content);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new UserNotFoundException(userId);
                }

                _logger.LogWarning("Unexpected response when validating user active status {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return false;
            }
            catch (UserNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user active status: {UserId}", userId);
                throw new ProductServiceCommunicationException("UserService", "ValidateUserActive", ex);
            }
        }

        public async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting user name: {UserId}", userId);

                var response = await _httpClient.GetAsync($"{_settings.BaseUrl}/api/users/{userId}/name", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonSerializer.Deserialize<string>(content) ?? "Unknown User";
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new UserNotFoundException(userId);
                }

                _logger.LogWarning("Unexpected response when getting user name {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return "Unknown User";
            }
            catch (UserNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user name: {UserId}", userId);
                throw new ProductServiceCommunicationException("UserService", "GetUserName", ex);
            }
        }
    }

    public class UserServiceSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
    }
}