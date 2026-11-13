using System.Threading.Channels;
using Grpc.Core;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.IntegrationTests;

/// <summary>Reads a StreamChanges call in the background so tests can await individual messages.</summary>
internal sealed class ChangeStream : IAsyncDisposable
{
    private readonly AsyncServerStreamingCall<Proto.ChangeStreamMessage> _call;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<Proto.ChangeStreamMessage> _messages = Channel.CreateUnbounded<Proto.ChangeStreamMessage>();
    private readonly Task _reader;

    public ChangeStream(Proto.AccessListSync.AccessListSyncClient client, Metadata credentials, long fromSequence)
    {
        _call = client.StreamChanges(new Proto.StreamChangesRequest { FromSequence = fromSequence }, credentials, cancellationToken: _cts.Token);
        _reader = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in _call.ResponseStream.ReadAllAsync(_cts.Token))
                {
                    await _messages.Writer.WriteAsync(message);
                }

                _messages.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                _messages.Writer.TryComplete(ex);
            }
        });
    }

    public async Task<Proto.AccessListChange> NextChangeAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var message = await _messages.Reader.ReadAsync(cts.Token);
            if (message.KindCase == Proto.ChangeStreamMessage.KindOneofCase.Change)
            {
                return message.Change;
            }
        }
    }

    public async Task<Proto.Heartbeat> NextHeartbeatAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (true)
        {
            var message = await _messages.Reader.ReadAsync(cts.Token);
            if (message.KindCase == Proto.ChangeStreamMessage.KindOneofCase.Heartbeat)
            {
                return message.Heartbeat;
            }
        }
    }

    public async Task<List<Proto.AccessListChange>> TakeChangesAsync(int count, TimeSpan? timeout = null)
    {
        var changes = new List<Proto.AccessListChange>(count);
        for (var i = 0; i < count; i++)
        {
            changes.Add(await NextChangeAsync(timeout));
        }

        return changes;
    }

    /// <summary>Waits for the call to fail and returns its status.</summary>
    public async Task<Status> FailureAsync()
    {
        try
        {
            await _reader;
            while (await _messages.Reader.WaitToReadAsync())
            {
                _messages.Reader.TryRead(out _);
            }

            return Status.DefaultSuccess;
        }
        catch (RpcException ex)
        {
            return ex.Status;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            await _reader;
        }
        catch (OperationCanceledException)
        {
            // Expected when the test ends the stream.
        }

        _call.Dispose();
        _cts.Dispose();
    }
}
