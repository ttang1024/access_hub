using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.Cards;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace AccessHub.Cardholder.UnitTests.Application;

public sealed class IssueCardHandlerTests
{
    private readonly ICardholderRepository _cardholders = Substitute.For<ICardholderRepository>();
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLog _audit = Substitute.For<IAuditLog>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task HandleAsync_UnknownCardholder_ReturnsNotFoundAndSavesNothing()
    {
        _cardholders.ExistsAsync(Arg.Any<CardholderId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler().HandleAsync(new IssueCardCommand(Guid.NewGuid(), "1234", null), TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NumberInUse_ReturnsConflict()
    {
        _cardholders.ExistsAsync(Arg.Any<CardholderId>(), Arg.Any<CancellationToken>()).Returns(true);
        _cards.NumberExistsAsync("ABCD", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().HandleAsync(new IssueCardCommand(Guid.NewGuid(), "abcd", null), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("Card.NumberInUse");
        _cards.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task HandleAsync_Valid_AddsCardAuditsAndSaves()
    {
        _cardholders.ExistsAsync(Arg.Any<CardholderId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().HandleAsync(new IssueCardCommand(Guid.NewGuid(), "abcd", null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _cards.Received(1).Add(Arg.Is<Card>(c => c.Number == "ABCD"));
        _audit.Received(1).Record("Card.Issued", nameof(Card), result.Value.ToString(), Arg.Is<object?>(o => o == null), Arg.Any<object>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private IssueCardHandler Handler() => new(_cardholders, _cards, _unitOfWork, _audit, _clock);
}
