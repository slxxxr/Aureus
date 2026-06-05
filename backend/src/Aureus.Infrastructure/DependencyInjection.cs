using Aureus.Infrastructure.Security;
using Aureus.UseCases.Common.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Aureus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
