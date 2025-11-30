namespace UserManagement.Application.Interfaces
{
    public interface IProductCommunicationService
    {
        Task ToggleUserProductsAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
        Task<int> GetUserProductsCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> CheckProductServiceHealthAsync(CancellationToken cancellationToken = default);
    }
}