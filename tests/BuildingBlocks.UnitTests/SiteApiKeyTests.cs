using AccessHub.BuildingBlocks.Application;

namespace AccessHub.BuildingBlocks.UnitTests;

public sealed class SiteApiKeyTests
{
    [Fact]
    public void Verify_GeneratedKey_MatchesItsHash()
    {
        var (plaintext, hash) = SiteApiKey.Generate();

        SiteApiKey.Verify(plaintext, hash).ShouldBeTrue();
        hash.Length.ShouldBe(32);
    }

    [Fact]
    public void Verify_DifferentKey_DoesNotMatch()
    {
        var (_, hash) = SiteApiKey.Generate();
        var (other, _) = SiteApiKey.Generate();

        SiteApiKey.Verify(other, hash).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!")]
    [InlineData("c2hvcnQ")]
    public void Verify_MalformedKey_DoesNotMatch(string presented)
    {
        var (_, hash) = SiteApiKey.Generate();

        SiteApiKey.Verify(presented, hash).ShouldBeFalse();
    }
}
