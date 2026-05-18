using RunRoutes.Core.Users;

namespace RunRoutes.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    public HashedPassword Hash(PlainPassword plain) =>
        HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(plain.Value));

    public bool Verify(PlainPassword plain, HashedPassword hashed) =>
        BCrypt.Net.BCrypt.Verify(plain.Value, hashed.Value);
}