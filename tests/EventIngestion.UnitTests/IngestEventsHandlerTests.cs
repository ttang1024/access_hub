using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.EventIngestion.Application;
using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Application.Features.IngestEvents;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace AccessHub.EventIngestion.UnitTests;

public sealed class IngestEventsHandlerTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task HandleAsync_RouteSiteDiffersFromAuthenticatedSite_ReturnsForbiddenAndStoresNothing()
    {
        var store = Substitute.For<IDoorEventStore>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new IngestEventsHandler(store, unitOfWork, Substitute.For<IIntegrationEventPublisher>(), new IngestionMetrics(new TestMeterFactory()), _clock);

        var result = await handler.HandleAsync(
            new IngestEventsCommand(Guid.NewGuid(), Guid.NewGuid(), [new IncomingDoorEvent(Guid.NewGuid(), Guid.NewGuid(), "DoorOpened", _clock.GetUtcNow(), null, null, null)]),
            TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.Forbidden);
        await unitOfWork.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync<IReadOnlyList<Domain.Events.DoorEvent>>(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Validator_UnknownTypeAndMissingReason_ReportsBothEvents()
    {
        var validator = new IngestEventsValidator(_clock);
        var command = new IngestEventsCommand(Guid.NewGuid(), Guid.NewGuid(),
        [
            new IncomingDoorEvent(Guid.NewGuid(), Guid.NewGuid(), "DoorExploded", _clock.GetUtcNow(), null, null, null),
            new IncomingDoorEvent(Guid.NewGuid(), Guid.NewGuid(), "AccessDenied", _clock.GetUtcNow(), "C1", null, null),
        ]);

        var errors = validator.Validate(command).Errors.Select(e => e.PropertyName).ToList();

        errors.ShouldContain("Events[0].Type");
        errors.ShouldContain("Events[1].DenyReason");
    }
}
