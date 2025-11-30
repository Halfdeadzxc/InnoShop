using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Exceptions;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Domain.Interfaces;
using Xunit;

namespace ProductManagement.UnitTests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IUserCommunicationService> _userCommunicationServiceMock;
        private readonly Mock<ILogger<ProductService>> _loggerMock;
        private readonly ProductService _productService;
        private readonly CancellationToken _cancellationToken;

        public ProductServiceTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _userCommunicationServiceMock = new Mock<IUserCommunicationService>();
            _loggerMock = new Mock<ILogger<ProductService>>();
            _cancellationToken = CancellationToken.None;

            _productService = new ProductService(
                _productRepositoryMock.Object,
                _userCommunicationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductExists_ReturnsProductDto()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = CreateTestProduct(productId);
            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.GetProductByIdAsync(productId, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal(product.Name, result.Name);
            Assert.Equal(product.Price, result.Price);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductNotFound_ThrowsProductNotFoundException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync((Product)null);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                _productService.GetProductByIdAsync(productId, _cancellationToken));
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductDeleted_ThrowsProductNotFoundException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = CreateTestProduct(productId, isDeleted: true);
            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                _productService.GetProductByIdAsync(productId, _cancellationToken));
        }

        [Fact]
        public async Task GetProductsAsync_ValidQuery_ReturnsPagedResponse()
        {
            // Arrange
            var query = new ProductQueryDto
            {
                Page = 1,
                PageSize = 10,
                Search = "test",
                MinPrice = 10,
                MaxPrice = 100,
                IsAvailable = true,
                UserId = Guid.NewGuid(),
                SortBy = "name",
                SortDescending = false
            };

            var products = new List<Product> { CreateTestProduct() };
            var totalCount = 1;

            _productRepositoryMock.Setup(x => x.GetPagedAsync(
                query.Page,
                query.PageSize,
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                query.IsAvailable,
                query.UserId,
                query.SortBy,
                query.SortDescending,
                _cancellationToken))
                .ReturnsAsync(products);

            _productRepositoryMock.Setup(x => x.GetCountAsync(
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                query.IsAvailable,
                query.UserId,
                _cancellationToken))
                .ReturnsAsync(totalCount);

            // Act
            var result = await _productService.GetProductsAsync(query, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(query.Page, result.Page);
            Assert.Equal(query.PageSize, result.PageSize);
        }

        [Fact]
        public async Task GetProductsByUserAsync_ValidUser_ReturnsProducts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var products = new List<Product> { CreateTestProduct(userId: userId) };

            _userCommunicationServiceMock.Setup(x => x.ValidateUserActiveAsync(userId, _cancellationToken))
                .ReturnsAsync(true);
            _productRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, _cancellationToken))
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetProductsByUserAsync(userId, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(userId, result.First().UserId);
        }

        [Fact]
        public async Task GetProductsByUserAsync_UserNotActive_ThrowsUserNotActiveException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userCommunicationServiceMock.Setup(x => x.ValidateUserActiveAsync(userId, _cancellationToken))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotActiveException>(() =>
                _productService.GetProductsByUserAsync(userId, _cancellationToken));
        }

        [Fact]
        public async Task CreateProductAsync_ValidData_CreatesProduct()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m,
                IsAvailable = true
            };

            _userCommunicationServiceMock.Setup(x => x.ValidateUserActiveAsync(userId, _cancellationToken))
                .ReturnsAsync(true);
            _productRepositoryMock.Setup(x => x.GetByNameAndUserAsync(createDto.Name, userId, _cancellationToken))
                .ReturnsAsync((Product)null);
            _productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>(), _cancellationToken))
                .ReturnsAsync((Product product, CancellationToken ct) => product); // Исправлено здесь

            // Act
            var result = await _productService.CreateProductAsync(userId, createDto, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Description, result.Description);
            Assert.Equal(createDto.Price, result.Price);
            Assert.Equal(userId, result.UserId);
            _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>(), _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CreateProductAsync_UserNotActive_ThrowsUserNotActiveException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateProductDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m
            };

            _userCommunicationServiceMock.Setup(x => x.ValidateUserActiveAsync(userId, _cancellationToken))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotActiveException>(() =>
                _productService.CreateProductAsync(userId, createDto, _cancellationToken));
        }

        [Fact]
        public async Task CreateProductAsync_DuplicateName_ThrowsProductNameConflictException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createDto = new CreateProductDto
            {
                Name = "Existing Product",
                Description = "Test Description",
                Price = 99.99m
            };
            var existingProduct = CreateTestProduct(userId: userId, name: createDto.Name);

            _userCommunicationServiceMock.Setup(x => x.ValidateUserActiveAsync(userId, _cancellationToken))
                .ReturnsAsync(true);
            _productRepositoryMock.Setup(x => x.GetByNameAndUserAsync(createDto.Name, userId, _cancellationToken))
                .ReturnsAsync(existingProduct);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNameConflictException>(() =>
                _productService.CreateProductAsync(userId, createDto, _cancellationToken));
        }

        [Fact]
        public async Task UpdateProductAsync_ValidData_UpdatesProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = CreateTestProduct(productId, userId);
            var updateDto = new UpdateProductDto
            {
                Name = "Updated Product",
                Description = "Updated Description",
                Price = 149.99m,
                IsAvailable = false
            };

            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);
            _productRepositoryMock.Setup(x => x.GetByNameAndUserAsync(updateDto.Name, userId, _cancellationToken))
                .ReturnsAsync((Product)null);
            _productRepositoryMock.Setup(x => x.UpdateAsync(product, _cancellationToken))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.UpdateProductAsync(productId, userId, updateDto, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.Description, result.Description);
            Assert.Equal(updateDto.Price, result.Price);
            Assert.Equal(updateDto.IsAvailable, result.IsAvailable);
            _productRepositoryMock.Verify(x => x.UpdateAsync(product, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ProductNotFound_ThrowsProductNotFoundException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var updateDto = new UpdateProductDto { Name = "Updated Product" };

            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync((Product)null);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                _productService.UpdateProductAsync(productId, userId, updateDto, _cancellationToken));
        }

        [Fact]
        public async Task UpdateProductAsync_UserNotOwner_ThrowsProductAccessDeniedException()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();
            var product = CreateTestProduct(productId, ownerUserId);
            var updateDto = new UpdateProductDto { Name = "Updated Product" };

            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);

            // Act & Assert
            await Assert.ThrowsAsync<ProductAccessDeniedException>(() =>
                _productService.UpdateProductAsync(productId, requesterUserId, updateDto, _cancellationToken));
        }

        [Fact]
        public async Task DeleteProductAsync_ValidProduct_SoftDeletesProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = CreateTestProduct(productId, userId);

            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);
            _productRepositoryMock.Setup(x => x.UpdateAsync(product, _cancellationToken))
                .ReturnsAsync(product);

            // Act
            await _productService.DeleteProductAsync(productId, userId, _cancellationToken);

            // Assert
            Assert.True(product.IsDeleted);
            _productRepositoryMock.Verify(x => x.UpdateAsync(product, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task ToggleProductStatusAsync_ValidProduct_TogglesStatus()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var product = CreateTestProduct(productId, userId, isAvailable: true);

            _productRepositoryMock.Setup(x => x.GetByIdAsync(productId, _cancellationToken))
                .ReturnsAsync(product);
            _productRepositoryMock.Setup(x => x.UpdateAsync(product, _cancellationToken))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.ToggleProductStatusAsync(productId, userId, _cancellationToken);

            // Assert
            Assert.False(result); // Status should be toggled from true to false
            Assert.False(product.IsAvailable);
            _productRepositoryMock.Verify(x => x.UpdateAsync(product, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task GetProductsCountAsync_WithQuery_ReturnsCount()
        {
            // Arrange
            var query = new ProductQueryDto
            {
                Search = "test",
                MinPrice = 10,
                MaxPrice = 100,
                IsAvailable = true,
                UserId = Guid.NewGuid()
            };
            var expectedCount = 5;

            _productRepositoryMock.Setup(x => x.GetCountAsync(
                query.Search,
                query.MinPrice,
                query.MaxPrice,
                query.IsAvailable,
                query.UserId,
                _cancellationToken))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _productService.GetProductsCountAsync(query, _cancellationToken);

            // Assert
            Assert.Equal(expectedCount, result);
        }

        [Fact]
        public async Task GetTotalProductsValueAsync_WithUserId_ReturnsTotalValue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var products = new List<Product>
            {
                CreateTestProduct(price: 100m),
                CreateTestProduct(price: 200m),
                CreateTestProduct(price: 300m)
            };

            _productRepositoryMock.Setup(x => x.GetPagedAsync(
                1, int.MaxValue, null, null, null, null, userId, null, false, _cancellationToken))
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetTotalProductsValueAsync(userId, _cancellationToken);

            // Assert
            Assert.Equal(600m, result); // 100 + 200 + 300
        }

        [Fact]
        public async Task BulkUpdateStatusAsync_ValidProducts_UpdatesAllProducts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var bulkUpdateDto = new BulkUpdateStatusDto
            {
                ProductIds = productIds,
                IsAvailable = false
            };
            var products = productIds.Select(id => CreateTestProduct(id, userId, isAvailable: true)).ToList();

            _productRepositoryMock.Setup(x => x.GetByIdsAsync(productIds, _cancellationToken))
                .ReturnsAsync(products);
            _productRepositoryMock.Setup(x => x.BulkUpdateAsync(It.IsAny<List<Product>>(), _cancellationToken))
                .Returns(Task.CompletedTask);

            // Act
            await _productService.BulkUpdateStatusAsync(userId, bulkUpdateDto, _cancellationToken);

            // Assert
            Assert.All(products, p => Assert.False(p.IsAvailable));
            _productRepositoryMock.Verify(x => x.BulkUpdateAsync(products, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task BulkUpdateStatusAsync_NoValidProducts_ThrowsProductNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productIds = new List<Guid> { Guid.NewGuid() };
            var bulkUpdateDto = new BulkUpdateStatusDto
            {
                ProductIds = productIds,
                IsAvailable = false
            };
            var products = new List<Product>(); // Empty list - no valid products

            _productRepositoryMock.Setup(x => x.GetByIdsAsync(productIds, _cancellationToken))
                .ReturnsAsync(products);

            // Act & Assert
            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                _productService.BulkUpdateStatusAsync(userId, bulkUpdateDto, _cancellationToken));
        }

        [Fact]
        public async Task GetRecentProductsAsync_ValidCount_ReturnsRecentProducts()
        {
            // Arrange
            var count = 5;
            var products = new List<Product>
            {
                CreateTestProduct(),
                CreateTestProduct(),
                CreateTestProduct()
            };

            _productRepositoryMock.Setup(x => x.GetRecentAsync(count, _cancellationToken))
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetRecentProductsAsync(count, _cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(products.Count, result.Count);
        }

        private Product CreateTestProduct(
            Guid? id = null,
            Guid? userId = null,
            string name = "Test Product",
            string description = "Test Description",
            decimal price = 99.99m,
            bool isAvailable = true,
            bool isDeleted = false)
        {
            return new Product
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = description,
                Price = price,
                IsAvailable = isAvailable,
                IsDeleted = isDeleted,
                UserId = userId ?? Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}