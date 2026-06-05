namespace Aureus.UseCases.Common.Security;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email);
}
