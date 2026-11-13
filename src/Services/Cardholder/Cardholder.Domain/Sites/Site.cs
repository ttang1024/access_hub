using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Sites;

/// <summary>A building with a site controller. Only the hash of its API key is stored.</summary>
public sealed class Site : AggregateRoot<SiteId>
{
    public const int MaxNameLength = 200;
    private const int HashLength = 32;

    private Site()
    {
    }

    private Site(SiteId id, string name, SiteTimeZone timeZone, byte[] apiKeyHash, DateTimeOffset now)
        : base(id)
    {
        Name = name;
        TimeZoneId = timeZone.Id;
        ApiKeyHash = apiKeyHash;
        CreatedAt = now;
    }

    public string Name { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = string.Empty;

    public byte[] ApiKeyHash { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Site> Register(string name, string timeZoneId, byte[] apiKeyHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return SiteErrors.InvalidName;
        }

        if (apiKeyHash.Length != HashLength)
        {
            return SiteErrors.InvalidKeyHash;
        }

        var timeZone = SiteTimeZone.Create(timeZoneId);
        if (timeZone.IsFailure)
        {
            return timeZone.Error!;
        }

        var site = new Site(SiteId.New(), name.Trim(), timeZone.Value, apiKeyHash, now);
        site.Apply(v => new SiteRegisteredDomainEvent(site.Id, site.Name, site.TimeZoneId, site.ApiKeyHash, v, now));
        return site;
    }

    public Result RotateApiKey(byte[] newApiKeyHash, DateTimeOffset now)
    {
        if (newApiKeyHash.Length != HashLength)
        {
            return SiteErrors.InvalidKeyHash;
        }

        ApiKeyHash = newApiKeyHash;
        Apply(v => new SiteApiKeyRotatedDomainEvent(Id, Name, TimeZoneId, ApiKeyHash, v, now));
        return Result.Success();
    }
}
