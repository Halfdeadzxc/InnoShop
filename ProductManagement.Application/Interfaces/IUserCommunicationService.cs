using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagement.Application.Interfaces
{
    public interface IUserCommunicationService
    {
        Task<bool> ValidateUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ValidateUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
