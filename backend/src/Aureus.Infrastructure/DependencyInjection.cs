using System.Text;
using Aureus.Infrastructure.Email;
using Aureus.Infrastructure.Email.Implementations;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Infrastructure.Security;
using Aureus.Infrastructure.Security.Implementations;
using Aureus.Infrastructure.Security.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Resend;

namespace Aureus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRegistrationTokenService, RegistrationTokenService>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));

        services.AddResend(opts =>
        {
            opts.ApiToken = configuration[$"{ResendOptions.SectionName}:ApiKey"] ?? string.Empty;
        });
        services.AddScoped<IEmailSender, ResendEmailSender>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
