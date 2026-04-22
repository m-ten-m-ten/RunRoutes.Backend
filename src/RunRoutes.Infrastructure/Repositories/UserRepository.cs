using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByIdForUpdateAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailForUpdateAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByActivationTokenForUpdateAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.ActivationToken == token);

    public Task<User?> GetByEmailChangeTokenForUpdateAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.EmailChangeToken == token);

    public Task<User?> GetByRefreshTokenForUpdateAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);

    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> ExistsByUsernameAsync(string username) =>
        db.Users.AnyAsync(u => u.Username == username);

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
