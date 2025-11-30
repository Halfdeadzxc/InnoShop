namespace ProductManagement.Application.Exceptions
{
    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException() : base("Product not found")
        {
        }

        public ProductNotFoundException(string message) : base(message)
        {
        }

        public ProductNotFoundException(Guid productId)
            : base($"Product with ID '{productId}' was not found.")
        {
        }

        public ProductNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}