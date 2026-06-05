using Aureus.Api.Filters;
using Aureus.Api.Middleware;
using Aureus.Infrastructure;
using Aureus.Postgres;
using Aureus.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUseCases();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddControllers(options => options.Filters.Add<UseCaseExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
