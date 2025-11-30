using Microsoft.EntityFrameworkCore;
using ProductManagement.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ProductManagement.Infrastructure.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(p => p.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(p => p.IsAvailable)
                    .IsRequired();

                entity.Property(p => p.UserId)
                    .IsRequired();

                entity.Property(p => p.IsDeleted)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.UpdatedAt)
                    .IsRequired(false);

                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.IsAvailable);
                entity.HasIndex(p => p.IsDeleted);
                entity.HasIndex(p => p.CreatedAt);
                entity.HasIndex(p => new { p.Name, p.UserId })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                entity.HasQueryFilter(p => !p.IsDeleted);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Product &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var product = (Product)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    product.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}