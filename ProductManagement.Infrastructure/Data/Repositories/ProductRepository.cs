using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;

namespace ProductManagement.Infrastructure.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting product by ID: {ProductId}", id);
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<List<Product>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products for user: {UserId}", userId);
            return await _context.Products
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Product>> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isAvailable = null,
            Guid? userId = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting paged products: Page {Page}, Size {PageSize}", page, pageSize);

            var query = _context.Products.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == isAvailable.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(p => p.UserId == userId.Value);
            }

            // Apply sorting
            query = ApplySorting(query, sortBy, sortDescending);

            // Apply pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return products;
        }

        public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Adding new product: {ProductName}", product.Name);

            await _context.Products.AddAsync(product, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return product;
        }

        public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating product: {ProductId}", product.Id);

            _context.Products.Update(product);
            await _context.SaveChangesAsync(cancellationToken);

            return product;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Hard deleting product: {ProductId}", id);

            var product = await GetByIdAsync(id, cancellationToken);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> GetCountAsync(
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isAvailable = null,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products count");

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == isAvailable.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(p => p.UserId == userId.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products by IDs: {ProductIds}", ids);

            return await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task BulkUpdateAsync(List<Product> products, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Bulk updating {Count} products", products.Count);

            _context.Products.UpdateRange(products);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Product?> GetByNameAndUserAsync(string name, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting product by name and user: {ProductName}, {UserId}", name, userId);

            return await _context.Products
                .FirstOrDefaultAsync(p => p.Name == name && p.UserId == userId, cancellationToken);
        }

        public async Task<List<Product>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting {Count} recent products", count);

            return await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Product>> SearchAsync(string searchTerm, int limit, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching products with term: {SearchTerm}", searchTerm);

            return await _context.Products
                .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, bool sortDescending)
        {
            return (sortBy?.ToLower(), sortDescending) switch
            {
                ("name", false) => query.OrderBy(p => p.Name),
                ("name", true) => query.OrderByDescending(p => p.Name),
                ("price", false) => query.OrderBy(p => p.Price),
                ("price", true) => query.OrderByDescending(p => p.Price),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),
                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                ("updatedat", false) => query.OrderBy(p => p.UpdatedAt),
                ("updatedat", true) => query.OrderByDescending(p => p.UpdatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}