using AccessHub.BuildingBlocks.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AccessHub.BuildingBlocks.Infrastructure.Auth;

/// <summary>Registers operator JWT authentication (Keycloak), optional site API key authentication, and the policies.</summary>
public static class AuthRegistration
{
    /// <summary>
    /// Adds JWT bearer validation against the Keycloak realm in <c>Auth:Authority</c> with audience <c>Auth:Audience</c>,
    /// and the admin/operator/viewer policies.
    /// </summary>
    public static AuthenticationBuilder AddAccessHubAuthentication(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("Auth");
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

        var authentication = builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = section["Authority"];
                options.Audience = section["Audience"] ?? "accesshub-api";
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = AuthConstants.Claims.Name;
                options.TokenValidationParameters.RoleClaimType = AuthConstants.Claims.Roles;
                if (section["ValidIssuer"] is { } issuer)
                {
                    // Inside containers the token issuer is the browser-facing Keycloak URL, not the internal one.
                    options.TokenValidationParameters.ValidIssuers = [issuer, $"{section["Authority"]}"];
                }

                options.Events = new JwtBearerEvents
                {
                    // Browsers cannot set headers on WebSocket requests; SignalR sends the token in the query string.
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.Policies.Admin, p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireRole(AuthConstants.Roles.Admin))
            .AddPolicy(AuthConstants.Policies.Operator, p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireRole(AuthConstants.Roles.Admin, AuthConstants.Roles.Operator))
            .AddPolicy(AuthConstants.Policies.Viewer, p => p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireRole(AuthConstants.Roles.Admin, AuthConstants.Roles.Operator, AuthConstants.Roles.Viewer))
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return authentication;
    }

    /// <summary>Adds the site API key scheme and the site policy. Requires an <see cref="ISiteApiKeyStore"/>.</summary>
    public static AuthenticationBuilder AddSiteApiKeyAuthentication(this AuthenticationBuilder authentication)
    {
        authentication.AddScheme<AuthenticationSchemeOptions, SiteApiKeyAuthenticationHandler>(AuthConstants.Schemes.SiteApiKey, null);
        authentication.Services.AddAuthorizationBuilder().AddPolicy(AuthConstants.Policies.Site, p => p
            .AddAuthenticationSchemes(AuthConstants.Schemes.SiteApiKey)
            .RequireClaim(AuthConstants.Claims.SiteId));
        return authentication;
    }

    /// <summary>The authenticated site's ID, or null if the caller is not a site.</summary>
    public static Guid? GetSiteId(this HttpContext context) =>
        Guid.TryParse(context.User.FindFirst(AuthConstants.Claims.SiteId)?.Value, out var id) ? id : null;
}
