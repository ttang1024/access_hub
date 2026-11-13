using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AccessHub.Testing;

/// <summary>Issues operator JWTs signed with a per-run test key; Keycloak is not started in tests.</summary>
public static class TestJwt
{
    public const string Issuer = "https://accesshub-tests.local/realms/accesshub";
    public const string Audience = "accesshub-api";

    private static readonly SymmetricSecurityKey Key = new(RandomNumberGenerator.GetBytes(32));

    public static string Create(string subject, params string[] roles)
    {
        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["preferred_username"] = subject,
                ["roles"] = roles,
            },
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256),
        });
    }

    /// <summary>Points JWT validation at the test key instead of Keycloak's metadata.</summary>
    public static IServiceCollection UseTestJwt(this IServiceCollection services) =>
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.Authority = null;
            o.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            o.TokenValidationParameters.ValidIssuer = Issuer;
            o.TokenValidationParameters.ValidIssuers = [Issuer];
            o.TokenValidationParameters.ValidAudience = Audience;
            o.TokenValidationParameters.IssuerSigningKey = Key;
        });
}
