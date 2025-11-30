namespace ProductManagement.Application.Exceptions
{
    public class UserNotActiveException : Exception
    {
        public UserNotActiveException() : base("User is not active")
        {
        }

        public UserNotActiveException(string message) : base(message)
        {
        }

        public UserNotActiveException(Guid userId)
            : base($"User with ID '{userId}' is not active.")
        {
        }

        public UserNotActiveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}