namespace ProductManagement.Application.Exceptions
{
    public class ProductNameConflictException : Exception
    {
        public string ProductName { get; }
        public Guid? ExistingProductId { get; }

        public ProductNameConflictException()
            : base("Product name conflict")
        {
            ProductName = string.Empty;
        }

        public ProductNameConflictException(string message)
            : base(message)
        {
            ProductName = string.Empty;
        }

        public ProductNameConflictException(string productName, Guid existingProductId)
            : base($"Product with name '{productName}' already exists (ID: {existingProductId})")
        {
            ProductName = productName;
            ExistingProductId = existingProductId;
        }

        public ProductNameConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
            ProductName = string.Empty;
        }
    }
}