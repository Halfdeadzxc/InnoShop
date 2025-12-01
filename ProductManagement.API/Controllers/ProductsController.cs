using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;

namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ICurrentUserService currentUserService,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts(
            [FromQuery] ProductQueryDto query,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting products with query: {@Query}", query);

            var products = await _productService.GetProductsAsync(query, cancellationToken);
            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProductById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting product by ID: {ProductId}", id);

            var product = await _productService.GetProductByIdAsync(id, cancellationToken);
            return Ok(product);
        }

        [HttpGet("my")]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductDto>>> GetMyProducts(
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Getting products for current user: {UserId}", userId);

            var products = await _productService.GetProductsByUserAsync(userId, cancellationToken);
            return Ok(products);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> CreateProduct(
            [FromBody] CreateProductDto createDto,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Creating product for user: {UserId}", userId);

            var product = await _productService.CreateProductAsync(userId, createDto, cancellationToken);

            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProductDto>> UpdateProduct(
            Guid id,
            [FromBody] UpdateProductDto updateDto,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Updating product {ProductId} by user {UserId}", id, userId);

            var product = await _productService.UpdateProductAsync(id, userId, updateDto, cancellationToken);
            return Ok(product);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProduct(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Deleting product {ProductId} by user {UserId}", id, userId);

            await _productService.DeleteProductAsync(id, userId, cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<bool>> ToggleProductStatus(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Toggling status for product {ProductId} by user {UserId}", id, userId);

            var newStatus = await _productService.ToggleProductStatusAsync(id, userId, cancellationToken);
            return Ok(new { isAvailable = newStatus });
        }

        [HttpPost("bulk-update-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpdateStatus(
            [FromBody] BulkUpdateStatusDto bulkUpdateDto,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetRequiredUserId();
            _logger.LogInformation("Bulk updating status for {Count} products by user {UserId}",
                bulkUpdateDto.ProductIds.Count, userId);

            await _productService.BulkUpdateStatusAsync(userId, bulkUpdateDto, cancellationToken);
            return Ok(new { message = "Products status updated successfully" });
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ProductDto>>> GetRecentProducts(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting {Count} recent products", count);

            var products = await _productService.GetRecentProductsAsync(count, cancellationToken);
            return Ok(products);
        }

        [HttpGet("count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> GetProductsCount(
            [FromQuery] ProductQueryDto? query = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting products count");

            var count = await _productService.GetProductsCountAsync(query, cancellationToken);
            return Ok(count);
        }

        [HttpPost("toggle-user-products")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleUserProducts(
            [FromBody] ToggleUserProductsRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Toggling products for user {UserId} to {IsActive}",
                request.UserId, request.IsActive);

            var currentUserId = _currentUserService.GetRequiredUserId();

            var userProducts = await _productService.GetProductsByUserAsync(request.UserId, cancellationToken);

            if (!userProducts.Any())
            {
                _logger.LogInformation("No products found for user {UserId}", request.UserId);
                return Ok(new { message = "No products found for user" });
            }

            var productIds = userProducts.Select(p => p.Id).ToList();
            var bulkUpdateDto = new BulkUpdateStatusDto
            {
                ProductIds = productIds,
                IsAvailable = request.IsActive
            };

            await _productService.BulkUpdateStatusAsync(request.UserId, bulkUpdateDto, cancellationToken);

            _logger.LogInformation("Successfully toggled {Count} products for user {UserId} to {IsActive}",
                productIds.Count, request.UserId, request.IsActive);

            return Ok(new
            {
                message = $"Successfully updated {productIds.Count} products",
                updatedCount = productIds.Count
            });
        }

        [HttpGet("user/{userId:guid}/count")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> GetUserProductsCount(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting products count for user {UserId}", userId);

            var products = await _productService.GetProductsByUserAsync(userId, cancellationToken);
            return Ok(products.Count);
        }

        [HttpGet("total-value")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        public async Task<ActionResult<decimal>> GetTotalProductsValue(
            [FromQuery] Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting total products value for user: {UserId}", userId);

            var totalValue = await _productService.GetTotalProductsValueAsync(userId, cancellationToken);
            return Ok(new { totalValue });
        }
    }
}