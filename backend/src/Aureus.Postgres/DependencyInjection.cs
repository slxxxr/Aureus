using Aureus.Postgres.Mappers;
using Aureus.Postgres.Implementations.Auth;
using Aureus.Postgres.Implementations.Workspaces;
using Aureus.UseCases.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aureus.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();

        return services;
    }
}
