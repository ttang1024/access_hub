using AccessHub.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;

namespace AccessHub.BuildingBlocks.Infrastructure.Auth;

/// <summary>The current user from the HTTP request's claims, or "system" outside a request.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private const string System = "system";

    /// <inheritdoc />
    public string UserId => accessor.HttpContext?.User.FindFirst(AuthConstants.Claims.Subject)?.Value
        ?? accessor.HttpContext?.User.FindFirst(AuthConstants.Claims.SiteId)?.Value
        ?? System;

    /// <inheritdoc />
    public string UserName => accessor.HttpContext?.User.Identity?.Name ?? System;
}
