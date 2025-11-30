using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Interfaces;

namespace UserManagement.Infrastructure.ExternalServices
{
    public class ProductCommunicationService : IProductCommunicationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductCommunicationService> _logger;

        public ProductCommunicationService(HttpClient httpClient, ILogger<ProductCommunicationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ToggleUserProductsAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new
                {
                    UserId = userId,
                    IsActive = isActive
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("api/products/toggle-user-products", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully toggled products for user {UserId} to {IsActive}",
                        userId, isActive);
                }
                else
                {
                    _logger.LogWarning("Failed to toggle products for user {UserId}. Status: {StatusCode}, Reason: {Reason}",
                        userId, response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while communicating with ProductService for user {UserId}", userId);
            }
            catch (TaskCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Operation cancelled while toggling products for user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while toggling products for user {UserId}", userId);
            }
        }

        public async Task<int> GetUserProductsCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/user/{userId}/count", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (int.TryParse(content, out var count))
                    {
                        return count;
                    }
                }

                _logger.LogWarning("Failed to get products count for user {UserId}. Status: {StatusCode}",
                    userId, response.StatusCode);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> CheckProductServiceHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}