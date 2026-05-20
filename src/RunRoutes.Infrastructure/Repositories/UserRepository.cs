using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByIdForUpdateAsync(Guid id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailForUpdateAsync(string email)
    {
        var vo = EmailAddress.Create(email);
        return db.Users.FirstOrDefaultAsync(u => u.Email == vo);
    }

    public Task<User?> GetByActivationTokenForUpdateAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.Activation != null && u.Activation.Value == token);

    public Task<User?> GetByEmailChangeTokenForUpdateAsync(string token) =>
        db.Users.FirstOrDefaultAsync(u => u.EmailChange != null && u.EmailChange.Token == token);

    public Task<bool> ExistsByEmailAsync(string email)
    {
        var vo = EmailAddress.Create(email);
        return db.Users.AnyAsync(u => u.Email == vo);
    }

    public Task<bool> ExistsByUsernameAsync(string username)
    {
        var vo = Username.Create(username);
        return db.Users.AnyAsync(u => u.Username == vo);
    }

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
