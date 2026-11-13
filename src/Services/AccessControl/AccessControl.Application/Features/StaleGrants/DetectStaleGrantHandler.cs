using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;

namespace AccessHub.AccessControl.Application.Features.StaleGrants;

/// <summary>
/// Catches grants made by offline or lagging sites. Works on a whole batch with two lookups, then publishes
/// <see cref="StaleAccessGranted"/> through the outbox at most once per source event, however often a grant is redelivered.
/// </summary>
public sealed class DetectStaleGrantHandler(IStaleGrantStore store, IIntegrationEventPublisher publisher, TimeProvider clock) : ICommandHandler<DoorEventsRecorded>
{
    public async Task<Result> HandleAsync(DoorEventsRecorded batch, CancellationToken cancellationToken)
    {
        var grants = batch.Type == DoorEventType.AccessGranted
            ? batch.Expand().Where(e => e.CardNumber is not null).ToList()
            : [];
        if (grants.Count == 0)
        {
            return Result.Success();
        }

        var cards = await store.FindCardsByNumbersAsync(grants.Select(g => g.CardNumber!).Distinct(StringComparer.Ordinal).ToList(), cancellationToken);
        var holders = cards.Count == 0
            ? new Dictionary<Guid, CardholderState>()
            : await store.FindCardholdersAsync(cards.Values.Select(c => c.CardholderId).Distinct().ToList(), cancellationToken);

        var now = clock.GetUtcNow();
        foreach (var grant in grants)
        {
            var card = cards.GetValueOrDefault(grant.CardNumber!);
            var holder = card is null ? null : holders.GetValueOrDefault(card.CardholderId);
            if (StaleGrantDetector.Check(card, holder, grant.OccurredAt) is not { } reason)
            {
                continue;
            }

            if (await store.TryRecordDetectionAsync(grant.SourceEventId, reason, now, cancellationToken))
            {
                await publisher.PublishAsync(
                    new StaleAccessGranted(Guid.CreateVersion7(), now, grant.CorrelationId, grant.SiteId, grant.DoorId, grant.CardNumber!, grant.SourceEventId, grant.OccurredAt, reason, grant.AccessListSequence),
                    cancellationToken);
            }
        }

        return Result.Success();
    }
}
