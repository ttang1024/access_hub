using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Application.Abstractions;

public sealed record SiteEntryRecord(AccessListEntryKind Kind, Guid EntityId, string Payload);
