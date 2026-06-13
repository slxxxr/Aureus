using Aureus.Api.Extensions;
using Aureus.Api.Filters;
using Aureus.Api.Middleware;
using Aureus.Infrastructure;
using Aureus.LLM;
using Aureus.Postgres;
using Aureus.UseCases;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUseCases();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddLlm(builder.Configuration);
builder.Services.AddMappings();
builder.Services.AddControllers(options => options.Filters.Add<UseCaseExceptionFilter>())
    .AddControllersAsServices()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddConfiguredSwagger();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Aureus.Persistence.AureusDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseConfiguredSwagger();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
