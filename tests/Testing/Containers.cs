using DotNet.Testcontainers.Configurations;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace AccessHub.Testing;

/// <summary>Image tags for test containers, matching the versions used by the AppHost.</summary>
public static class Containers
{
    public const string PostgresImage = "postgres:17.11-alpine";
    public const string RabbitMqImage = "rabbitmq:4.2-management";

    public static PostgreSqlContainer Postgres() => new PostgreSqlBuilder(PostgresImage).Build();

    public static RabbitMqContainer RabbitMq() => new RabbitMqBuilder(RabbitMqImage).Build();

    /// <summary>Must match <c>DELAYED_EXCHANGE_VERSION</c> in deploy/rabbitmq/Dockerfile.</summary>
    public const string DelayedExchangeVersion = "4.2.0";

    /// <summary>
    /// RabbitMQ with the delayed message exchange plugin, equivalent to deploy/rabbitmq. The plugin and an
    /// enabled_plugins file are copied into the stock image before start, instead of building an image, because
    /// image builds through the Docker API fail on Docker Desktop's containerd image store.
    /// </summary>
    public static async Task<RabbitMqContainer> RabbitMqWithDelayedExchangeAsync()
    {
        var plugin = await DownloadDelayedExchangePluginAsync();
        const UnixFileModes readable = UnixFileModes.UserRead | UnixFileModes.UserWrite | UnixFileModes.GroupRead | UnixFileModes.OtherRead;
        return new RabbitMqBuilder(RabbitMqImage)
            .WithResourceMapping(plugin, $"/opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-{DelayedExchangeVersion}.ez", fileMode: readable)
            .WithResourceMapping(
                System.Text.Encoding.ASCII.GetBytes("[rabbitmq_management,rabbitmq_prometheus,rabbitmq_delayed_message_exchange]."),
                "/etc/rabbitmq/enabled_plugins",
                fileMode: readable)
            .Build();
    }

    private static async Task<byte[]> DownloadDelayedExchangePluginAsync()
    {
        var cache = Path.Combine(Path.GetTempPath(), $"rabbitmq_delayed_message_exchange-{DelayedExchangeVersion}.ez");
        if (!File.Exists(cache))
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(
                $"https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v{DelayedExchangeVersion}/rabbitmq_delayed_message_exchange-{DelayedExchangeVersion}.ez");
            await File.WriteAllBytesAsync(cache, bytes);
        }

        return await File.ReadAllBytesAsync(cache);
    }
}
