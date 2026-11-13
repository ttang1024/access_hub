using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Sites;

public static class SiteErrors
{
    public static Error NotFound(SiteId id) => Error.NotFound("Site.NotFound", $"Site {id} was not found.");

    public static Error InvalidTimeZone(string? id) => Error.Validation("Site.InvalidTimeZone", $"'{id}' is not a known IANA time zone.");

    public static Error InvalidName => Error.Validation("Site.InvalidName", "Site name is required and at most 200 characters.");

    public static Error InvalidKeyHash => Error.Validation("Site.InvalidKeyHash", "An API key hash must be a 32-byte SHA-256 hash.");
}
