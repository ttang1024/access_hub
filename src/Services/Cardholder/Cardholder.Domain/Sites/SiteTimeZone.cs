using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Sites;

/// <summary>An IANA time zone ID that the runtime can resolve.</summary>
public sealed record SiteTimeZone : IValueObject
{
    private SiteTimeZone(string id) => Id = id;

    public string Id { get; }

    public static Result<SiteTimeZone> Create(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
        {
            return SiteErrors.InvalidTimeZone(id);
        }

        // Normalise Windows IDs to IANA so every consumer sees the same form.
        if (!zone.HasIanaId && TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId))
        {
            return new SiteTimeZone(ianaId);
        }

        return new SiteTimeZone(zone.Id);
    }
}
