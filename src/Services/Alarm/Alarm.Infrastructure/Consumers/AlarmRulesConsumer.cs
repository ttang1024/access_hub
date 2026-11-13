using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.AccessControl;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Consumers;

public sealed class AlarmRulesConsumer(ICommandHandler<DoorEventsRecorded> doorEvents, ICommandHandler<StaleAccessGranted> staleGrants)
    : IConsumer<DoorEventsRecorded>, IConsumer<StaleAccessGranted>
{
    public async Task Consume(ConsumeContext<DoorEventsRecorded> context) =>
        Ensure(await doorEvents.HandleAsync(context.Message, context.CancellationToken));

    public async Task Consume(ConsumeContext<StaleAccessGranted> context) =>
        Ensure(await staleGrants.HandleAsync(context.Message, context.CancellationToken));

    private static void Ensure(BuildingBlocks.Domain.Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
        }
    }
}
