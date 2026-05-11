namespace RunRoutes.Core.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByIdForUpdateAsync(Guid id);
    Task<User?> GetByEmailForUpdateAsync(string email);
    Task<User?> GetByActivationTokenForUpdateAsync(string token);
    Task<User?> GetByEmailChangeTokenForUpdateAsync(string token);
    Task<User?> GetByRefreshTokenForUpdateAsync(string token);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
