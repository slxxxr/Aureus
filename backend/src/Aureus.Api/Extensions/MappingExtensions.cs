using Aureus.Api.Mappers;
using Aureus.Postgres.Mappers;

namespace Aureus.Api.Extensions;

public static class MappingExtensions
{
    public static IServiceCollection AddMappings(this IServiceCollection services)
    {
        services.AddAutoMapper(configuration =>
        {
            configuration.AddProfile<DatabaseMappings>();
            configuration.AddProfile<ContractMappings>();
        });

        return services;
    }
}
