using System.Security.Claims;
using System.Text.Encodings.Web;
using AccessHub.BuildingBlocks.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessHub.BuildingBlocks.Infrastructure.Auth;

/// <summary>Authenticates site controllers by <c>X-Site-Id</c> and <c>X-Api-Key</c> against the stored key hash.</summary>
public sealed class SiteApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISiteApiKeyStore keyStore) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthConstants.Headers.SiteId, out var siteIdValue) ||
            !Request.Headers.TryGetValue(AuthConstants.Headers.ApiKey, out var apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        if (!Guid.TryParse(siteIdValue, out var siteId))
        {
            return AuthenticateResult.Fail("Invalid site ID.");
        }

        var storedHash = await keyStore.GetKeyHashAsync(siteId, Context.RequestAborted);
        if (storedHash is null || !SiteApiKey.Verify(apiKey.ToString(), storedHash))
        {
            return AuthenticateResult.Fail("Invalid site credentials.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(AuthConstants.Claims.SiteId, siteId.ToString()), new Claim(ClaimTypes.Name, $"site:{siteId}")],
            Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
