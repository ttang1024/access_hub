using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Messages;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure;

/// <summary>
/// Request/response with the saga, so the API can answer 200, 404 or 409 synchronously. Uses the bus-level client
/// factory: a lifecycle command is a request, not an integration event, and must not wait for an outbox flush.
/// </summary>
internal sealed class AlarmCommandSender(IClientFactory clients) : IAlarmCommandSender
{
    private static readonly RequestTimeout Timeout = RequestTimeout.After(s: 15);

    public Task<(AlarmCommandAccepted? Accepted, AlarmCommandRejected? Rejected)> AcknowledgeAsync(AcknowledgeAlarm command, CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    public Task<(AlarmCommandAccepted? Accepted, AlarmCommandRejected? Rejected)> CloseAsync(CloseAlarm command, CancellationToken cancellationToken) =>
        SendAsync(command, cancellationToken);

    private async Task<(AlarmCommandAccepted?, AlarmCommandRejected?)> SendAsync<T>(T command, CancellationToken cancellationToken)
        where T : class
    {
        var response = await clients.CreateRequestClient<T>(Timeout).GetResponse<AlarmCommandAccepted, AlarmCommandRejected>(command, cancellationToken);
        if (response.Is(out Response<AlarmCommandAccepted>? accepted))
        {
            return (accepted.Message, null);
        }

        return response.Is(out Response<AlarmCommandRejected>? rejected)
            ? (null, rejected.Message)
            : throw new InvalidOperationException("The alarm saga sent an unexpected response.");
    }
}
