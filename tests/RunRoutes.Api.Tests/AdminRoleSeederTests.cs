using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunRoutes.Core.Settings;
using RunRoutes.Core.Users;
using RunRoutes.Infrastructure.Data;
using RunRoutes.Infrastructure.Services;

namespace RunRoutes.Api.Tests;

public class AdminRoleSeederTests
{
    private static AppDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static User MakeUser(string email, UserRole role = UserRole.User)
    {
        // "user" プレフィックスで予約語 ("admin" 等) を回避
        var username = $"user{email.Split('@')[0].ToLowerInvariant()}";
        return TestUserBuilder.CreateActivated(email, username, "password123", role);
    }

    [Fact]
    public async Task 空配列なら何もしない()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(MakeUser("anyone@example.com"));
        await db.SaveChangesAsync();

        var seeder = new AdminRoleSeeder(
            db,
            Options.Create(new AdminSettings { AdminEmails = [] }),
            NullLogger<AdminRoleSeeder>.Instance);

        await seeder.RunAsync();

        var user = await db.Users.FirstAsync();
        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public async Task 一致するメールをAdminに昇格する()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(MakeUser("promote@example.com"));
        db.Users.Add(MakeUser("other@example.com"));
        await db.SaveChangesAsync();

        var seeder = new AdminRoleSeeder(
            db,
            Options.Create(new AdminSettings { AdminEmails = ["promote@example.com"] }),
            NullLogger<AdminRoleSeeder>.Instance);

        await seeder.RunAsync();

        Assert.Equal(UserRole.Admin,
            (await db.Users.FirstAsync(u => u.Email.Value == "promote@example.com")).Role);
        Assert.Equal(UserRole.User,
            (await db.Users.FirstAsync(u => u.Email.Value == "other@example.com")).Role);
    }

    [Fact]
    public async Task 大文字小文字を無視して一致する()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        db.Users.Add(MakeUser("Mixed@Example.com"));
        await db.SaveChangesAsync();

        var seeder = new AdminRoleSeeder(
            db,
            Options.Create(new AdminSettings { AdminEmails = ["MIXED@example.com"] }),
            NullLogger<AdminRoleSeeder>.Instance);

        await seeder.RunAsync();

        Assert.Equal(UserRole.Admin, (await db.Users.FirstAsync()).Role);
    }

    [Fact]
    public async Task 既にAdminならスキップする()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        var user = MakeUser("admin@example.com", UserRole.Admin);
        var initialUpdatedAt = user.UpdatedAt;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var seeder = new AdminRoleSeeder(
            db,
            Options.Create(new AdminSettings { AdminEmails = ["admin@example.com"] }),
            NullLogger<AdminRoleSeeder>.Instance);

        await seeder.RunAsync();

        var after = await db.Users.FirstAsync();
        Assert.Equal(UserRole.Admin, after.Role);
        Assert.Equal(initialUpdatedAt, after.UpdatedAt);
    }

    [Fact]
    public async Task マッチしないメールは例外にしない()
    {
        var db = CreateDb(Guid.NewGuid().ToString());
        await db.SaveChangesAsync();

        var seeder = new AdminRoleSeeder(
            db,
            Options.Create(new AdminSettings { AdminEmails = ["nobody@example.com"] }),
            NullLogger<AdminRoleSeeder>.Instance);

        await seeder.RunAsync();
    }
}
