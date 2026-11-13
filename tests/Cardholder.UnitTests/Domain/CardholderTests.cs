using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cardholders;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.UnitTests.Domain;

public sealed class CardholderTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_DuplicateGroups_StoresDistinctAndIsActive()
    {
        var group = AccessGroupId.New();

        var cardholder = CardholderAggregate.Create("Ada", "Lovelace", [group, group], Now).Value;

        cardholder.IsActive.ShouldBeTrue();
        cardholder.AccessGroupIds.ShouldBe([group]);
        cardholder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CardholderChangedDomainEvent>().Version.ShouldBe(1);
    }

    [Fact]
    public void Update_NoActualChange_RaisesNothing()
    {
        var cardholder = CardholderAggregate.Create("Ada", "Lovelace", [], Now).Value;
        cardholder.ClearDomainEvents();

        cardholder.Update("Ada", "Lovelace", true, [], null, Now).IsSuccess.ShouldBeTrue();

        cardholder.DomainEvents.ShouldBeEmpty();
        cardholder.Version.ShouldBe(1);
    }

    [Fact]
    public void Update_Deactivate_RaisesEventWithFullState()
    {
        var group = AccessGroupId.New();
        var cardholder = CardholderAggregate.Create("Ada", "Lovelace", [group], Now).Value;
        cardholder.ClearDomainEvents();

        cardholder.Update("Ada", "Lovelace", false, [group], 1, Now);

        var changed = cardholder.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<CardholderChangedDomainEvent>();
        changed.IsActive.ShouldBeFalse();
        changed.AccessGroupIds.ShouldBe([group]);
        changed.Version.ShouldBe(2);
    }

    [Fact]
    public void Update_WrongExpectedVersion_ReturnsConflict()
    {
        var cardholder = CardholderAggregate.Create("Ada", "Lovelace", [], Now).Value;

        var result = cardholder.Update("Ada", "Byron", true, [], 7, Now);

        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        cardholder.LastName.ShouldBe("Lovelace");
    }

    [Fact]
    public void Create_BlankName_ReturnsValidationError() =>
        CardholderAggregate.Create(" ", "Lovelace", [], Now).Error.ShouldBe(CardholderErrors.InvalidName);
}
