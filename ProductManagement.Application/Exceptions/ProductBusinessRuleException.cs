namespace ProductManagement.Application.Exceptions
{
    public class ProductBusinessRuleException : Exception
    {
        public ProductBusinessRuleException() : base("Product business rule violation")
        {
        }

        public ProductBusinessRuleException(string message) : base(message)
        {
        }

        public ProductBusinessRuleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}