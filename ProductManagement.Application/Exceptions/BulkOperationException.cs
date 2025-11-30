using System.Collections.Generic;

namespace ProductManagement.Application.Exceptions
{
    public class BulkOperationException : Exception
    {
        public List<Guid> FailedProductIds { get; }
        public string OperationType { get; }

        public BulkOperationException()
            : base("Bulk operation failed")
        {
            FailedProductIds = new List<Guid>();
            OperationType = "Unknown";
        }

        public BulkOperationException(string message)
            : base(message)
        {
            FailedProductIds = new List<Guid>();
            OperationType = "Unknown";
        }

        public BulkOperationException(string operationType, List<Guid> failedProductIds)
            : base($"Bulk operation '{operationType}' failed for {failedProductIds.Count} products")
        {
            OperationType = operationType;
            FailedProductIds = failedProductIds;
        }

        public BulkOperationException(string operationType, List<Guid> failedProductIds, Exception innerException)
            : base($"Bulk operation '{operationType}' failed for {failedProductIds.Count} products", innerException)
        {
            OperationType = operationType;
            FailedProductIds = failedProductIds;
        }

        public BulkOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
            FailedProductIds = new List<Guid>();
            OperationType = "Unknown";
        }
    }
}