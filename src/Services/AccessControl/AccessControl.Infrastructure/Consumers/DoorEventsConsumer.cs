using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.AccessControl.Infrastructure.Consumers;

/// <summary>Checks a batch of grants for staleness in one transaction (the inbox deduplicates the batch).</summary>
public sealed class DoorEventsConsumer(ICommandHandler<DoorEventsRecorded> detectStaleGrants) : IConsumer<DoorEventsRecorded>
{
    public async Task Consume(ConsumeContext<DoorEventsRecorded> context)
    {
        var result = await detectStaleGrants.HandleAsync(context.Message, context.CancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
        }
    }
}
