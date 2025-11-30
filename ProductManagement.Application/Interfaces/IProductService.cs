using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PagedResponse<ProductDto>> GetProductsAsync(ProductQueryDto query, CancellationToken cancellationToken = default);
        Task<List<ProductDto>> GetProductsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<ProductDto> CreateProductAsync(Guid userId, CreateProductDto createDto, CancellationToken cancellationToken = default);
        Task<ProductDto> UpdateProductAsync(Guid id, Guid userId, UpdateProductDto updateDto, CancellationToken cancellationToken = default);
        Task DeleteProductAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ToggleProductStatusAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetProductsCountAsync(ProductQueryDto? query = null, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalProductsValueAsync(Guid? userId = null, CancellationToken cancellationToken = default);
        Task BulkUpdateStatusAsync(Guid userId, BulkUpdateStatusDto bulkUpdateDto, CancellationToken cancellationToken = default);
        Task<List<ProductDto>> GetRecentProductsAsync(int count = 10, CancellationToken cancellationToken = default);
    }
}