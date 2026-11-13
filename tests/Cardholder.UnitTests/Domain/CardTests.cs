using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;

namespace AccessHub.Cardholder.UnitTests.Domain;

public sealed class CardTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_ValidInput_RaisesCardIssuedAtVersion1()
    {
        var card = Card.Issue(CardholderId.New(), " ab12cd ", Now.AddDays(30), Now).Value;

        card.Number.ShouldBe("AB12CD");
        card.Version.ShouldBe(1);
        var issued = card.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CardIssuedDomainEvent>();
        issued.Version.ShouldBe(1);
        issued.ExpiresAt.ShouldBe(Now.AddDays(30));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12-34-56")]
    [InlineData("123456789012345678901234567890123")]
    public void Issue_InvalidNumber_ReturnsValidationError(string number)
    {
        var result = Card.Issue(CardholderId.New(), number, null, Now);

        result.Error.ShouldBe(CardErrors.InvalidNumber);
    }

    [Fact]
    public void Issue_ExpiryInPast_ReturnsValidationError()
    {
        var result = Card.Issue(CardholderId.New(), "1234", Now.AddSeconds(-1), Now);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Revoke_ActiveCard_SetsRevokedAtAndIncrementsVersion()
    {
        var card = Card.Issue(CardholderId.New(), "1234", null, Now).Value;
        card.ClearDomainEvents();

        card.Revoke(Now.AddHours(1)).IsSuccess.ShouldBeTrue();

        card.IsRevoked.ShouldBeTrue();
        card.Version.ShouldBe(2);
        card.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CardRevokedDomainEvent>().RevokedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ReturnsConflictAndRaisesNothing()
    {
        var card = Card.Issue(CardholderId.New(), "1234", null, Now).Value;
        card.Revoke(Now);
        card.ClearDomainEvents();

        var result = card.Revoke(Now);

        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        card.DomainEvents.ShouldBeEmpty();
        card.Version.ShouldBe(2);
    }
}
