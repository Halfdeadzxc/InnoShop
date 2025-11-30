namespace ProductManagement.Application.Exceptions
{
    public class ProductUpdateException : Exception
    {
        public ProductUpdateException() : base("Failed to update product")
        {
        }

        public ProductUpdateException(string message) : base(message)
        {
        }

        public ProductUpdateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ProductUpdateException(Guid productId)
            : base($"Failed to update product with ID '{productId}'")
        {
        }
    }
}