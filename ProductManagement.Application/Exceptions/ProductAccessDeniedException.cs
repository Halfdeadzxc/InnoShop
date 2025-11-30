namespace ProductManagement.Application.Exceptions
{
    public class ProductAccessDeniedException : Exception
    {
        public ProductAccessDeniedException() : base("Access to product denied")
        {
        }

        public ProductAccessDeniedException(string message) : base(message)
        {
        }

        public ProductAccessDeniedException(Guid productId, Guid userId)
            : base($"User '{userId}' does not have access to product '{productId}'")
        {
        }

        public ProductAccessDeniedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}