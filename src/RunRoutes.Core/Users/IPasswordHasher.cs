namespace RunRoutes.Core.Users;

public interface IPasswordHasher
{
    HashedPassword Hash(PlainPassword plain);
    bool Verify(PlainPassword plain, HashedPassword hashed);
}