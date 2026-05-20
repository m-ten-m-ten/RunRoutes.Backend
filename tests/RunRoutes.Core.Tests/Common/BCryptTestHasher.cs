using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Common;

public class BCryptTestHasher : IPasswordHasher
{
    public HashedPassword Hash(PlainPassword plain) =>
        HashedPassword.FromHash(BCrypt.Net.BCrypt.HashPassword(plain.Value));

    public bool Verify(PlainPassword plain, HashedPassword hashed) =>
        BCrypt.Net.BCrypt.Verify(plain.Value, hashed.Value);
}
