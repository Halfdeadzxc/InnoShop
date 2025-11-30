using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ProductDbContext context)
        {
            if (!context.Products.Any())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "Sample Product 1",
                        Description = "This is a sample product description",
                        Price = 29.99m,
                        IsAvailable = true,
                        UserId = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = "Sample Product 2",
                        Description = "Another sample product description",
                        Price = 49.99m,
                        IsAvailable = true,
                        UserId = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }
}