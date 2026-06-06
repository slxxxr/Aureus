using Aureus.Postgres;
using Aureus.Postgres.Mappers;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Aureus.IntegrationTests.Common;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public IMapper Mapper { get; } = BuildMapper();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AureusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AureusDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new AureusDbContext(options);
    }

    private static IMapper BuildMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(configuration => configuration.AddProfile<DatabaseMappings>());

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
