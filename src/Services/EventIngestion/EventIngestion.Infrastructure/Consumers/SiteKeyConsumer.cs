using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using MassTransit;

namespace AccessHub.EventIngestion.Infrastructure.Consumers;

public sealed class SiteKeyConsumer(ICommandHandler<SiteRegistered> registered, ICommandHandler<SiteApiKeyRotated> rotated)
    : IConsumer<SiteRegistered>, IConsumer<SiteApiKeyRotated>
{
    public async Task Consume(ConsumeContext<SiteRegistered> context) =>
        Ensure(await registered.HandleAsync(context.Message, context.CancellationToken));

    public async Task Consume(ConsumeContext<SiteApiKeyRotated> context) =>
        Ensure(await rotated.HandleAsync(context.Message, context.CancellationToken));

    private static void Ensure(BuildingBlocks.Domain.Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
        }
    }
}
