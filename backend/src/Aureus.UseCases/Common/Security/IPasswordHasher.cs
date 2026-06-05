namespace Aureus.UseCases.Common.Security;

public interface IPasswordHasher
{
    string Hash(string password);
}
