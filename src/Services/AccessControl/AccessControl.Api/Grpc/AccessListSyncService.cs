using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Features.Snapshot;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Proto = AccessHub.Contracts.Grpc.Sync.V1;

namespace AccessHub.AccessControl.Api.Grpc;

/// <summary>
/// Site-facing sync. The site is identified by its API key, never by a request field, so a site can only read its own list.
/// </summary>
[Authorize(Policy = AuthConstants.Policies.Site)]
public sealed class AccessListSyncService(
    IQueryHandler<GetSnapshotQuery, SiteSnapshot> snapshots,
    SiteChangeStreamer streamer,
    IOptions<SyncOptions> options,
    IHostApplicationLifetime lifetime,
    TimeProvider clock) : Proto.AccessListSync.AccessListSyncBase
{
    public override async Task GetSnapshot(Proto.GetSnapshotRequest request, IServerStreamWriter<Proto.SnapshotChunk> responseStream, ServerCallContext context)
    {
        var siteId = SiteId(context);
        var result = await snapshots.HandleAsync(new GetSnapshotQuery(siteId), context.CancellationToken);
        if (result.IsFailure)
        {
            throw ToRpcException(result.Error!);
        }

        var snapshot = result.Value;
        var chunks = snapshot.Entries.Chunk(options.Value.SnapshotChunkSize).ToList();
        if (chunks.Count == 0)
        {
            chunks.Add([]);
        }

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = new Proto.SnapshotChunk { TimeZoneId = snapshot.TimeZoneId, IsLast = i == chunks.Count - 1 };
            chunk.Entries.AddRange(chunks[i].Select(e => ProtoMapper.ToProto(e.Kind, e.EntityId, e.Payload)));
            if (chunk.IsLast)
            {
                chunk.Sequence = snapshot.Sequence;
            }

            await responseStream.WriteAsync(chunk, context.CancellationToken);
        }
    }

    public override async Task StreamChanges(Proto.StreamChangesRequest request, IServerStreamWriter<Proto.ChangeStreamMessage> responseStream, ServerCallContext context)
    {
        // On shutdown, end open streams cleanly (status OK) instead of holding the host until it aborts them;
        // sites reconnect, with backoff, to another replica.
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        var result = await streamer.StreamAsync(
            SiteId(context),
            request.FromSequence,
            (item, ct) => responseStream.WriteAsync(
                item.Change is { } change
                    ? new Proto.ChangeStreamMessage { Change = ProtoMapper.ToProto(change) }
                    : new Proto.ChangeStreamMessage { Heartbeat = new Proto.Heartbeat { LatestSequence = item.HeartbeatSequence!.Value, SentAt = Timestamp.FromDateTimeOffset(clock.GetUtcNow()) } },
                ct),
            stopping.Token);
        if (result.IsFailure)
        {
            throw ToRpcException(result.Error!);
        }
    }

    private static Guid SiteId(ServerCallContext context) =>
        context.GetHttpContext().GetSiteId() ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Site credentials required."));

    private static RpcException ToRpcException(Error error) => new(new Status(
        error.Type switch
        {
            ErrorType.NotFound => StatusCode.NotFound,
            ErrorType.Failure => StatusCode.FailedPrecondition,
            ErrorType.Validation => StatusCode.InvalidArgument,
            ErrorType.Forbidden => StatusCode.PermissionDenied,
            _ => StatusCode.Internal,
        },
        $"{error.Code}: {error.Message}"));
}
