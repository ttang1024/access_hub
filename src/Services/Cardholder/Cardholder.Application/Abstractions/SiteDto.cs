namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record SiteDto(Guid Id, string Name, string TimeZoneId, DateTimeOffset CreatedAt, int Version);
