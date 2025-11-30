using ProductManagement.Domain.Entities;

namespace ProductManagement.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Product>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Product>> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isAvailable = null,
            Guid? userId = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default);
        Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
        Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isAvailable = null,
            Guid? userId = null,
            CancellationToken cancellationToken = default);
        Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
        Task BulkUpdateAsync(List<Product> products, CancellationToken cancellationToken = default);
        Task<Product?> GetByNameAndUserAsync(string name, Guid userId, CancellationToken cancellationToken = default);
        Task<List<Product>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
        Task<List<Product>> SearchAsync(string searchTerm, int limit, CancellationToken cancellationToken = default);
    }
}