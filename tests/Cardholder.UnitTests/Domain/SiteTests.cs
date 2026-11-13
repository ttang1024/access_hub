using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.UnitTests.Domain;

public sealed class SiteTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_UnknownTimeZone_ReturnsValidationError() =>
        Site.Register("HQ", "Mars/Olympus", new byte[32], Now).Error!.Code.ShouldBe("Site.InvalidTimeZone");

    [Fact]
    public void Register_ShortHash_ReturnsValidationError() =>
        Site.Register("HQ", "UTC", new byte[8], Now).Error.ShouldBe(SiteErrors.InvalidKeyHash);

    [Fact]
    public void RotateApiKey_NewHash_RaisesRotatedWithFullState()
    {
        var site = Site.Register("HQ", "Europe/Paris", new byte[32], Now).Value;
        site.ClearDomainEvents();
        var newHash = Enumerable.Repeat((byte)7, 32).ToArray();

        site.RotateApiKey(newHash, Now);

        var rotated = site.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SiteApiKeyRotatedDomainEvent>();
        rotated.ApiKeyHash.ShouldBe(newHash);
        rotated.TimeZoneId.ShouldBe("Europe/Paris");
        rotated.Version.ShouldBe(2);
    }
}
