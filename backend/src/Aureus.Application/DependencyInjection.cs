using Microsoft.Extensions.DependencyInjection;

namespace Aureus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
