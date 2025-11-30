using Microsoft.Extensions.Logging;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Exceptions;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IUserCommunicationService _userCommunicationService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            IUserCommunicationService userCommunicationService,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _userCommunicationService = userCommunicationService;
            _logger = logger;
        }

        public async Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting product by ID: {ProductId}", id);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null || product.IsDeleted)
            {
                throw new ProductNotFoundException(id);
            }

            _logger.LogDebug("Product found: {ProductId}", id);
            return MapToDto(product);
        }

        public async Task<PagedResponse<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting products with query: {@Query}", query);

            var products = await _productRepository.GetPagedAsync(
                query.Page,
                query.PageSize,
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                query.IsAvailable,
                query.UserId,
                query.SortBy,
                query.SortDescending,
                cancellationToken);

            var totalCount = await _productRepository.GetCountAsync(
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                query.IsAvailable,
                query.UserId,
                cancellationToken);

            _logger.LogDebug("Retrieved {Count} products out of {TotalCount}", products.Count, totalCount);

            return new PagedResponse<ProductDto>
            {
                Items = products.Select(MapToDto).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<ProductDto>> GetProductsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting products for user: {UserId}", userId);

            if (!await _userCommunicationService.ValidateUserActiveAsync(userId, cancellationToken))
            {
                throw new UserNotActiveException(userId);
            }

            var products = await _productRepository.GetByUserIdAsync(userId, cancellationToken);
            _logger.LogDebug("Retrieved {Count} products for user {UserId}", products.Count, userId);

            return products.Select(MapToDto).ToList();
        }

        public async Task<ProductDto> CreateProductAsync(Guid userId, CreateProductDto createDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating product for user: {UserId}", userId);

            if (!await _userCommunicationService.ValidateUserActiveAsync(userId, cancellationToken))
            {
                throw new UserNotActiveException(userId);
            }

            var existingProduct = await _productRepository.GetByNameAndUserAsync(createDto.Name, userId, cancellationToken);
            if (existingProduct != null && !existingProduct.IsDeleted)
            {
                throw new ProductNameConflictException(createDto.Name, existingProduct.Id);
            }

            var product = new Product
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Price = createDto.Price,
                IsAvailable = createDto.IsAvailable,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var createdProduct = await _productRepository.AddAsync(product, cancellationToken);
            _logger.LogInformation("Product {ProductId} created successfully by user {UserId}", createdProduct.Id, userId);

            return MapToDto(createdProduct);
        }

        public async Task<ProductDto> UpdateProductAsync(Guid id, Guid userId, UpdateProductDto updateDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating product {ProductId} by user {UserId}", id, userId);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null || product.IsDeleted)
            {
                throw new ProductNotFoundException(id);
            }

            if (product.UserId != userId)
            {
                throw new ProductAccessDeniedException(id, userId);
            }

            if (!string.IsNullOrEmpty(updateDto.Name) && updateDto.Name != product.Name)
            {
                var existingProduct = await _productRepository.GetByNameAndUserAsync(updateDto.Name, userId, cancellationToken);
                if (existingProduct != null && !existingProduct.IsDeleted && existingProduct.Id != id)
                {
                    throw new ProductNameConflictException(updateDto.Name, existingProduct.Id);
                }
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
                product.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.Description))
                product.Description = updateDto.Description;

            if (updateDto.Price.HasValue)
                product.Price = updateDto.Price.Value;

            if (updateDto.IsAvailable.HasValue)
                product.IsAvailable = updateDto.IsAvailable.Value;

            product.UpdateTimestamps();

            var updatedProduct = await _productRepository.UpdateAsync(product, cancellationToken);
            _logger.LogInformation("Product {ProductId} updated successfully by user {UserId}", id, userId);

            return MapToDto(updatedProduct);
        }

        public async Task DeleteProductAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting product {ProductId} by user {UserId}", id, userId);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null || product.IsDeleted)
            {
                throw new ProductNotFoundException(id);
            }

            if (product.UserId != userId)
            {
                throw new ProductAccessDeniedException(id, userId);
            }

            product.IsDeleted = true;
            product.UpdateTimestamps();

            await _productRepository.UpdateAsync(product, cancellationToken);
            _logger.LogInformation("Product {ProductId} soft deleted successfully by user {UserId}", id, userId);
        }

        public async Task<bool> ToggleProductStatusAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Toggling status for product {ProductId} by user {UserId}", id, userId);

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null || product.IsDeleted)
            {
                throw new ProductNotFoundException(id);
            }

            if (product.UserId != userId)
            {
                throw new ProductAccessDeniedException(id, userId);
            }

            product.IsAvailable = !product.IsAvailable;
            product.UpdateTimestamps();

            await _productRepository.UpdateAsync(product, cancellationToken);
            _logger.LogInformation("Product {ProductId} status toggled to {Status} by user {UserId}",
                id, product.IsAvailable, userId);

            return product.IsAvailable;
        }

        public async Task<int> GetProductsCountAsync(ProductQueryDto? query = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products count with query: {@Query}", query);

            var count = await _productRepository.GetCountAsync(
                query?.Search,
                query?.MinPrice,
                query?.MaxPrice,
                query?.IsAvailable,
                query?.UserId,
                cancellationToken);

            return count;
        }

        public async Task<decimal> GetTotalProductsValueAsync(Guid? userId = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting total products value for user: {UserId}", userId);

            var products = await _productRepository.GetPagedAsync(1, int.MaxValue, null, null, null, null, userId, null, false, cancellationToken);
            return products.Sum(p => p.Price);
        }

        public async Task BulkUpdateStatusAsync(Guid userId, BulkUpdateStatusDto bulkUpdateDto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bulk updating status for {Count} products by user {UserId}",
                bulkUpdateDto.ProductIds.Count, userId);

            var products = await _productRepository.GetByIdsAsync(bulkUpdateDto.ProductIds, cancellationToken);
            var userProducts = products.Where(p => p.UserId == userId && !p.IsDeleted).ToList();

            if (userProducts.Count == 0)
            {
                throw new ProductNotFoundException("No valid products found for bulk update");
            }

            var failedProductIds = new List<Guid>();

            foreach (var product in userProducts)
            {
                try
                {
                    product.IsAvailable = bulkUpdateDto.IsAvailable;
                    product.UpdateTimestamps();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update product {ProductId} in bulk operation", product.Id);
                    failedProductIds.Add(product.Id);
                }
            }

            var successfulProducts = userProducts.Where(p => !failedProductIds.Contains(p.Id)).ToList();
            if (successfulProducts.Any())
            {
                await _productRepository.BulkUpdateAsync(successfulProducts, cancellationToken);
            }

            if (failedProductIds.Any())
            {
                throw new BulkOperationException("StatusUpdate", failedProductIds);
            }

            _logger.LogInformation("Bulk updated {Count} products status to {Status} by user {UserId}",
                userProducts.Count, bulkUpdateDto.IsAvailable, userId);
        }

        public async Task<List<ProductDto>> GetRecentProductsAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting {Count} recent products", count);

            var products = await _productRepository.GetRecentAsync(count, cancellationToken);
            return products.Select(MapToDto).ToList();
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                IsAvailable = product.IsAvailable,
                UserId = product.UserId,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}