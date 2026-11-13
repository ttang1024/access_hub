using AccessHub.BuildingBlocks.Domain;
using AccessHub.EventIngestion.Domain.Events;
using AccessHub.EventIngestion.Domain.Sites;

namespace AccessHub.EventIngestion.UnitTests;

public sealed class DoorEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DoorEventKind.AccessGranted)]
    [InlineData(DoorEventKind.AccessDenied)]
    public void Create_DecisionWithoutCard_ReturnsValidationError(DoorEventKind kind) =>
        Create(kind, card: null, reason: "UnknownCard").Error!.Code.ShouldBe("DoorEvent.MissingCard");

    [Theory]
    [InlineData(null)]
    [InlineData("Because")]
    public void Create_DeniedWithInvalidReason_ReturnsValidationError(string? reason) =>
        Create(DoorEventKind.AccessDenied, "C1", reason).Error!.Code.ShouldBe("DoorEvent.InvalidDenyReason");

    [Fact]
    public void Create_FarInFuture_ReturnsValidationError() =>
        Create(DoorEventKind.DoorOpened, occurredAt: Now.AddMinutes(10)).Error!.Type.ShouldBe(ErrorType.Validation);

    [Fact]
    public void Create_SmallClockSkewAndOldEvents_AreAccepted()
    {
        Create(DoorEventKind.DoorOpened, occurredAt: Now.AddMinutes(4)).IsSuccess.ShouldBeTrue();
        Create(DoorEventKind.DoorOpened, occurredAt: Now.AddDays(-3)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_GrantedWithReason_DropsReasonAndNormalisesCard()
    {
        var e = Create(DoorEventKind.AccessGranted, " abc1 ", "CardRevoked").Value;

        e.DenyReason.ShouldBeNull();
        e.CardNumber.ShouldBe("ABC1");
    }

    [Fact]
    public void KnownSite_OlderVersion_IsIgnored()
    {
        var site = KnownSite.Create(Guid.NewGuid());
        site.Apply([1], 2).ShouldBeTrue();

        site.Apply([2], 1).ShouldBeFalse();

        site.ApiKeyHash.ShouldBe(new byte[] { 1 });
    }

    private static Result<DoorEvent> Create(DoorEventKind kind, string? card = null, string? reason = null, DateTimeOffset? occurredAt = null) =>
        DoorEvent.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), kind, occurredAt ?? Now.AddMinutes(-1), card, reason, 3, Now);
}
