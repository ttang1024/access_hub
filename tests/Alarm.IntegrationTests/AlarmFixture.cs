using AccessHub.Alarm.Infrastructure;
using AccessHub.Alarm.Infrastructure.Persistence;
using AccessHub.Contracts.IntegrationEvents.Alarm;
using AccessHub.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(AccessHub.Alarm.IntegrationTests.AlarmFixture))]

namespace AccessHub.Alarm.IntegrationTests;

public sealed class AlarmFixture : ServiceFixture<Program, AlarmDbContext>
{
    public static readonly TimeSpan EscalationDelay = TimeSpan.FromSeconds(3);
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.4.8-alpine").Build();

    protected override string DatabaseConnectionName => AlarmInfrastructureRegistration.ConnectionName;

    protected override IEnumerable<Type> ListenedEventTypes =>
        [typeof(AlarmRaised), typeof(AlarmEscalated), typeof(AlarmAcknowledged), typeof(AlarmClosed)];

    public async Task<HubConnection> ConnectHubAsync(string role = "viewer")
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Factory.Server.BaseAddress, "/hubs/alarms"), o =>
            {
                o.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                o.Transports = HttpTransportType.LongPolling;
                o.AccessTokenProvider = () => Task.FromResult<string?>(TestJwt.Create($"test-{role}", role));
            })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected override Task<RabbitMqContainer> CreateRabbitMqAsync() => Containers.RabbitMqWithDelayedExchangeAsync();

    protected override Task StartDependenciesAsync() => _redis.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:redis", _redis.GetConnectionString());
        builder.UseSetting("Alarms:EscalationDelay", EscalationDelay.ToString());
    }
}
