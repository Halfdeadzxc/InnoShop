namespace ProductManagement.Application.Exceptions
{
    public class PriceValidationException : Exception
    {
        public decimal InvalidPrice { get; }

        public PriceValidationException()
            : base("Price validation failed")
        {
            InvalidPrice = 0;
        }

        public PriceValidationException(string message)
            : base(message)
        {
            InvalidPrice = 0;
        }

        public PriceValidationException(decimal invalidPrice)
            : base($"Price validation failed for value: {invalidPrice}")
        {
            InvalidPrice = invalidPrice;
        }

        public PriceValidationException(decimal invalidPrice, string message)
            : base(message)
        {
            InvalidPrice = invalidPrice;
        }

        public PriceValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            InvalidPrice = 0;
        }
    }
}