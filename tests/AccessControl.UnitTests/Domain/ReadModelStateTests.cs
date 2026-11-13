using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;

namespace AccessHub.AccessControl.UnitTests.Domain;

public sealed class ReadModelStateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Apply_SameOrOlderVersion_IsIgnored()
    {
        var card = CardState.Create(Guid.NewGuid());
        card.Apply(Guid.NewGuid(), "C1", null, T0, 2).ShouldBeTrue();

        card.Apply(Guid.NewGuid(), "C1", null, null, 1).ShouldBeFalse();
        card.Apply(Guid.NewGuid(), "C1", null, null, 2).ShouldBeFalse();

        card.IsRevoked.ShouldBeTrue();
        card.Version.ShouldBe(2);
    }

    [Fact]
    public void CardholderApply_BecomesInactive_RecordsInactiveSince()
    {
        var holder = CardholderState.Create(Guid.NewGuid());
        holder.Apply(true, [], 1, T0);

        holder.Apply(false, [], 2, T0.AddHours(1));

        holder.WasInactiveAt(T0.AddMinutes(59)).ShouldBeFalse();
        holder.WasInactiveAt(T0.AddHours(1)).ShouldBeTrue();
    }

    [Fact]
    public void CardholderApply_InactiveToInactive_KeepsOriginalInactiveSince()
    {
        var holder = CardholderState.Create(Guid.NewGuid());
        holder.Apply(false, [], 1, T0);

        holder.Apply(false, [Guid.NewGuid()], 2, T0.AddDays(1));

        holder.InactiveSince.ShouldBe(T0);
    }

    [Fact]
    public void ForCard_CardholderInGroupWithDoorAtSite_ListsOnlyThatSitesGroups()
    {
        var site = Guid.NewGuid();
        var (here, elsewhere) = (Group(site), Group(Guid.NewGuid()));
        var holder = CardholderState.Create(Guid.NewGuid());
        holder.Apply(true, [here.AccessGroupId, elsewhere.AccessGroupId], 1, T0);
        var card = CardState.Create(Guid.NewGuid());
        card.Apply(holder.CardholderId, "C1", null, null, 1);

        var entry = SiteEntryCalculator.ForCard(card, holder, Dict(here, elsewhere), site);

        entry.ShouldNotBeNull().AccessGroupIds.ShouldBe([here.AccessGroupId]);
    }

    [Fact]
    public void ForCard_NoGroupAtSiteOrUnknownCardholder_IsNotInList()
    {
        var site = Guid.NewGuid();
        var elsewhere = Group(Guid.NewGuid());
        var holder = CardholderState.Create(Guid.NewGuid());
        holder.Apply(true, [elsewhere.AccessGroupId], 1, T0);
        var card = CardState.Create(Guid.NewGuid());
        card.Apply(holder.CardholderId, "C1", null, null, 1);

        SiteEntryCalculator.ForCard(card, holder, Dict(elsewhere), site).ShouldBeNull();
        SiteEntryCalculator.ForCard(card, null, Dict(elsewhere), site).ShouldBeNull();
    }

    [Fact]
    public void ForCard_RevokedCardAndInactiveCardholder_StayInListWithState()
    {
        var site = Guid.NewGuid();
        var group = Group(site);
        var holder = CardholderState.Create(Guid.NewGuid());
        holder.Apply(false, [group.AccessGroupId], 1, T0);
        var card = CardState.Create(Guid.NewGuid());
        card.Apply(holder.CardholderId, "C1", null, T0, 2);

        var entry = SiteEntryCalculator.ForCard(card, holder, Dict(group), site).ShouldNotBeNull();

        entry.IsRevoked.ShouldBeTrue();
        entry.CardholderActive.ShouldBeFalse();
    }

    [Fact]
    public void ForGroup_DeletedOrNoDoorsAtSite_IsNotInList()
    {
        var site = Guid.NewGuid();
        var group = Group(site);
        SiteEntryCalculator.ForGroup(group, Guid.NewGuid()).ShouldBeNull();
        SiteEntryCalculator.ForGroup(group, site).ShouldNotBeNull();

        group.Apply("G", isDeleted: true, [], 2);

        SiteEntryCalculator.ForGroup(group, site).ShouldBeNull();
    }

    private static AccessGroupState Group(Guid siteId)
    {
        var group = AccessGroupState.Create(Guid.NewGuid());
        group.Apply("G", false, [new GroupDoorState(Guid.NewGuid(), siteId, [new ScheduleWindow(DayOfWeek.Monday, 0, 60)])], 1);
        return group;
    }

    private static Dictionary<Guid, AccessGroupState> Dict(params AccessGroupState[] groups) => groups.ToDictionary(g => g.AccessGroupId);
}
