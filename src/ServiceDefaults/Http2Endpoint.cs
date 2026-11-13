using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

#pragma warning disable IDE0130 // Aspire convention: extension methods live in Microsoft.Extensions.Hosting.
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

/// <summary>
/// Kestrel cannot serve HTTP/2 without TLS on an endpoint that also serves HTTP/1.1 (there is no protocol negotiation
/// in cleartext), so gRPC gets its own HTTP/2-only port, set by <c>GRPC_PORT</c>.
/// </summary>
public static class Http2Endpoint
{
    /// <summary>Configuration key for the HTTP/2-only gRPC port.</summary>
    public const string GrpcPortKey = "GRPC_PORT";

    /// <summary>
    /// When <c>GRPC_PORT</c> is set, listens on the configured URLs as usual plus an HTTP/2-only port.
    /// Explicit listen calls replace the URL configuration, so the normal URLs are re-bound here.
    /// </summary>
    public static WebApplicationBuilder UseDedicatedGrpcPort(this WebApplicationBuilder builder)
    {
        if (!int.TryParse(builder.Configuration[GrpcPortKey], out var grpcPort))
        {
            return builder;
        }

        var urls = (builder.Configuration["urls"] ?? "http://localhost:5000")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(u => new Uri(u.Replace("://*", "://0.0.0.0", StringComparison.Ordinal).Replace("://+", "://0.0.0.0", StringComparison.Ordinal)))
            .ToList();
        var anyIp = urls.Any(u => u.Host is "0.0.0.0" or "[::]");

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var url in urls)
            {
                Listen(kestrel, url.Host, url.Port, HttpProtocols.Http1AndHttp2);
            }

            Listen(kestrel, anyIp ? "0.0.0.0" : "localhost", grpcPort, HttpProtocols.Http2);
        });
        return builder;
    }

    private static void Listen(KestrelServerOptions kestrel, string host, int port, HttpProtocols protocols)
    {
        if (host is "localhost")
        {
            kestrel.ListenLocalhost(port, o => o.Protocols = protocols);
        }
        else if (host is "0.0.0.0" or "[::]")
        {
            kestrel.ListenAnyIP(port, o => o.Protocols = protocols);
        }
        else
        {
            kestrel.Listen(IPAddress.Parse(host.Trim('[', ']')), port, o => o.Protocols = protocols);
        }
    }
}
