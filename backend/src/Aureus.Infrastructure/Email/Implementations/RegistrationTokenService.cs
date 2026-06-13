using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aureus.Infrastructure.Email.Interfaces;
using Aureus.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aureus.Infrastructure.Email.Implementations;

public sealed class RegistrationTokenService(
    IOptions<JwtOptions> jwtOptions,
    ILogger<RegistrationTokenService> logger) : IRegistrationTokenService
{
    private const string TokenType = "registration";
    private const string TokenTypeClaim = "token_type";
    private const string PurposeClaim = "purpose";
    private static readonly TimeSpan s_tokenLifetime = TimeSpan.FromMinutes(15);

    private readonly SymmetricSecurityKey _signingKey =
        new(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret));

    public string Generate(string email, string purpose)
    {
        var handler = new JwtSecurityTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(PurposeClaim, purpose),
                new Claim(TokenTypeClaim, TokenType),
            ]),
            Expires = DateTime.UtcNow.Add(s_tokenLifetime),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public RegistrationTokenPayload? TryValidate(string token)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.Zero,
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);

            var tokenType = principal.FindFirstValue(TokenTypeClaim);
            if (tokenType != TokenType)
            {
                return null;
            }

            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
            var purpose = principal.FindFirstValue(PurposeClaim);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(purpose))
            {
                return null;
            }

            return new RegistrationTokenPayload(email, purpose);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Registration token validation failed");
            return null;
        }
    }
}
