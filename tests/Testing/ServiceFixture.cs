using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace AccessHub.Testing;

/// <summary>
/// One real Postgres, one real RabbitMQ and one service host per test assembly. Tests isolate themselves by data
/// (fresh IDs), not by host, which keeps the suite fast.
/// </summary>
public abstract class ServiceFixture<TProgram, TDbContext> : IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
    private readonly PostgreSqlContainer _postgres = Containers.Postgres();
    private RabbitMqContainer _rabbitMq = null!;

    public WebApplicationFactory<TProgram> Factory { get; private set; } = null!;

    public PublishedEventsListener Events { get; private set; } = null!;

    public string RabbitMqUri => _rabbitMq.GetConnectionString();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    protected abstract string DatabaseConnectionName { get; }

    /// <summary>Event types the listener should capture.</summary>
    protected abstract IEnumerable<Type> ListenedEventTypes { get; }

    public HttpClient CreateClient(string? role = null)
    {
        var client = Factory.CreateClient();
        return role is null ? client : client.As(role);
    }

    public async Task<T> WithDbAsync<T>(Func<TDbContext, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TDbContext>());
    }

    public virtual async ValueTask InitializeAsync()
    {
        _rabbitMq = await CreateRabbitMqAsync();
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), StartDependenciesAsync());
        Events = await PublishedEventsListener.StartAsync(RabbitMqUri, ListenedEventTypes, CancellationToken.None);

        Factory = new WebApplicationFactory<TProgram>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting($"ConnectionStrings:{DatabaseConnectionName}", _postgres.GetConnectionString());
            builder.UseSetting("ConnectionStrings:messaging", _rabbitMq.GetConnectionString());
            ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.UseTestJwt();
                ConfigureTestServices(services);
            });
        });

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TDbContext>().Database.MigrateAsync();
        }

        await OnStartedAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Events.DisposeAsync();
        await Factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask());
    }

    /// <summary>The broker; override to use an image with plugins.</summary>
    protected virtual Task<RabbitMqContainer> CreateRabbitMqAsync() => Task.FromResult(Containers.RabbitMq());

    /// <summary>Starts extra containers (for example Redis) before the host is built.</summary>
    protected virtual Task StartDependenciesAsync() => Task.CompletedTask;

    protected virtual void ConfigureWebHost(IWebHostBuilder builder)
    {
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected virtual Task OnStartedAsync() => Task.CompletedTask;
}
