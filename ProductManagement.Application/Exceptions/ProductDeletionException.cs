namespace ProductManagement.Application.Exceptions
{
    public class ProductDeletionException : Exception
    {
        public ProductDeletionException() : base("Failed to delete product")
        {
        }

        public ProductDeletionException(string message) : base(message)
        {
        }

        public ProductDeletionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ProductDeletionException(Guid productId)
            : base($"Failed to delete product with ID '{productId}'")
        {
        }
    }
}