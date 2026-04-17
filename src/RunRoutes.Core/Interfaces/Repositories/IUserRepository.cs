using RunRoutes.Core.Entities;

namespace RunRoutes.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByActivationTokenAsync(string token);
    Task<User?> GetByEmailChangeTokenAsync(string token);
    Task<User?> GetByRefreshTokenAsync(string token);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
