using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Domain.AccessGroups;

/// <summary>A door an access group opens, and when. The site is kept so consumers know which site lists change.</summary>
public sealed record DoorAccess(DoorId DoorId, SiteId SiteId, Schedule Schedule) : IValueObject;
