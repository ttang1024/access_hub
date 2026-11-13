namespace AccessHub.SiteSimulator.Connectivity;

/// <summary>Fails requests while the simulated link is down, as a real network failure would.</summary>
public sealed class ConnectivityHandler(NetworkConnectivity connectivity, TimeProvider clock) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!connectivity.IsOnline(clock))
        {
            throw new HttpRequestException("Simulated outage: the cloud is unreachable.", null, System.Net.HttpStatusCode.ServiceUnavailable);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
