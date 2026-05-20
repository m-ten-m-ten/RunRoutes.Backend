using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Common;

public class FakePasswordHasher : IPasswordHasher
{
    public HashedPassword Hash(PlainPassword plain) =>
        HashedPassword.FromHash($"$2a$fake${plain.Value}");

    public bool Verify(PlainPassword plain, HashedPassword hashed) =>
        hashed.Value == $"$2a$fake${plain.Value}";
}
