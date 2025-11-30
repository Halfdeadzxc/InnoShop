namespace ProductManagement.Application.Exceptions
{
    public class ProductCreationException : Exception
    {
        public ProductCreationException() : base("Failed to create product")
        {
        }

        public ProductCreationException(string message) : base(message)
        {
        }

        public ProductCreationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ProductCreationException(Guid userId)
            : base($"Failed to create product for user '{userId}'")
        {
        }
    }
}