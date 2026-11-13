using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.UnitTests.Domain;

public sealed class SiteAccessListTests
{
    [Fact]
    public void Upsert_CardNumberChanged_OldNumberNoLongerFound()
    {
        var list = new SiteAccessList("UTC");
        var id = Guid.NewGuid();
        list.Upsert(new CardEntry(id, "OLD1", Guid.NewGuid(), false, null, true, []));

        list.Upsert(new CardEntry(id, "NEW1", Guid.NewGuid(), false, null, true, []));

        list.FindCard("OLD1").ShouldBeNull();
        list.FindCard("NEW1").ShouldNotBeNull();
        list.Cards.Count.ShouldBe(1);
    }

    [Fact]
    public void Remove_Card_RemovesBothIndexes()
    {
        var list = new SiteAccessList("UTC");
        var id = Guid.NewGuid();
        list.Upsert(new CardEntry(id, "C1", Guid.NewGuid(), false, null, true, []));

        list.Remove(AccessListEntryKind.Card, id);

        list.FindCard("C1").ShouldBeNull();
        list.Cards.ShouldBeEmpty();
    }
}
