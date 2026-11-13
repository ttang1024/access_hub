using System.Net.Http.Headers;

namespace AccessHub.Testing;

public static class HttpClientExtensions
{
    public static HttpClient As(this HttpClient client, string role)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create($"test-{role}", role));
        return client;
    }
}
