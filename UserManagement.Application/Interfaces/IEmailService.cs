namespace UserManagement.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendConfirmationEmailAsync(string email, string token, CancellationToken cancellationToken = default);
        Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken = default);
        Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default);
    }
}