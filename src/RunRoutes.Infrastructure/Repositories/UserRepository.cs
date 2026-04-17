using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByActivationTokenAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.ActivationToken == token);

    public Task<User?> GetByEmailChangeTokenAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.EmailChangeToken == token);

    public Task<User?> GetByRefreshTokenAsync(string token) =>
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
