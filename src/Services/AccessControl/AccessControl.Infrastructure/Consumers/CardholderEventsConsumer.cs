using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using MassTransit;

namespace AccessHub.AccessControl.Infrastructure.Consumers;

/// <summary>
/// Projects Cardholder events into the read model and site change logs. The inbox deduplicates redelivered messages;
/// the version check and the recompute-and-diff projection make every event naturally idempotent as well.
/// </summary>
public sealed class CardholderEventsConsumer(
    ICommandHandler<SiteRegistered> siteRegistered,
    ICommandHandler<SiteApiKeyRotated> siteKeyRotated,
    ICommandHandler<DoorRegistered> doorRegistered,
    ICommandHandler<AccessGroupChanged> accessGroupChanged,
    ICommandHandler<CardholderChanged> cardholderChanged,
    ICommandHandler<CardIssued> cardIssued,
    ICommandHandler<CardRevoked> cardRevoked)
    : IConsumer<SiteRegistered>, IConsumer<SiteApiKeyRotated>, IConsumer<DoorRegistered>, IConsumer<AccessGroupChanged>,
      IConsumer<CardholderChanged>, IConsumer<CardIssued>, IConsumer<CardRevoked>
{
    public Task Consume(ConsumeContext<SiteRegistered> context) => RunAsync(siteRegistered, context);

    public Task Consume(ConsumeContext<SiteApiKeyRotated> context) => RunAsync(siteKeyRotated, context);

    public Task Consume(ConsumeContext<DoorRegistered> context) => RunAsync(doorRegistered, context);

    public Task Consume(ConsumeContext<AccessGroupChanged> context) => RunAsync(accessGroupChanged, context);

    public Task Consume(ConsumeContext<CardholderChanged> context) => RunAsync(cardholderChanged, context);

    public Task Consume(ConsumeContext<CardIssued> context) => RunAsync(cardIssued, context);

    public Task Consume(ConsumeContext<CardRevoked> context) => RunAsync(cardRevoked, context);

    private static async Task RunAsync<T>(ICommandHandler<T> handler, ConsumeContext<T> context)
        where T : class
    {
        var result = await handler.HandleAsync(context.Message, context.CancellationToken);
        if (result.IsFailure)
        {
            // Never swallow: a projection that cannot apply goes through retry and then to the error queue.
            throw new InvalidOperationException($"Projection of {typeof(T).Name} failed: {result.Error!.Code} {result.Error.Message}");
        }
    }
}
