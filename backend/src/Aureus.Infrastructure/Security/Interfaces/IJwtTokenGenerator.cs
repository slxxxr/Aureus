namespace Aureus.Infrastructure.Security.Interfaces;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email);
}
