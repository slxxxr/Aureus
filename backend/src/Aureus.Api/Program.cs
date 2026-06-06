using Aureus.Api.Extensions;
using Aureus.Api.Filters;
using Aureus.Api.Mappers;
using Aureus.Api.Middleware;
using Aureus.Infrastructure;
using Aureus.Postgres;
using Aureus.Postgres.Mappers;
using Aureus.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUseCases();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddAutoMapper(configuration =>
{
    configuration.AddProfile<DatabaseMappings>();
    configuration.AddProfile<ContractMappings>();
});
builder.Services.AddControllers(options => options.Filters.Add<UseCaseExceptionFilter>())
    .AddControllersAsServices();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddConfiguredSwagger();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseConfiguredSwagger();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
