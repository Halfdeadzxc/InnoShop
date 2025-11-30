namespace ProductManagement.Application.Exceptions
{
    public class ProductServiceCommunicationException : Exception
    {
        public string ServiceName { get; }
        public string Operation { get; }

        public ProductServiceCommunicationException()
            : base("Product service communication error")
        {
            ServiceName = "Unknown";
            Operation = "Unknown";
        }

        public ProductServiceCommunicationException(string message)
            : base(message)
        {
            ServiceName = "Unknown";
            Operation = "Unknown";
        }

        public ProductServiceCommunicationException(string serviceName, string operation)
            : base($"Communication error with service '{serviceName}' during operation '{operation}'")
        {
            ServiceName = serviceName;
            Operation = operation;
        }

        public ProductServiceCommunicationException(string serviceName, string operation, Exception innerException)
            : base($"Communication error with service '{serviceName}' during operation '{operation}'", innerException)
        {
            ServiceName = serviceName;
            Operation = operation;
        }

        public ProductServiceCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
            ServiceName = "Unknown";
            Operation = "Unknown";
        }
    }
}