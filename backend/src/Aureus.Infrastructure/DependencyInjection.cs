using Aureus.Infrastructure.Mappers;
using Aureus.Infrastructure.Persistence;
using Aureus.Infrastructure.Security;
using Aureus.UseCases.Common.Persistence;
using Aureus.UseCases.Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aureus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres") ?? string.Empty;

        services.AddDbContext<AureusDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddAutoMapper(configuration =>
        {
            configuration.AddProfile<DatabaseMappings>();
        });
        services.AddScoped<IUserRegistrationDb, UserRegistrationDb>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
