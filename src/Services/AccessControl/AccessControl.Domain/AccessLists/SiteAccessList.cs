namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>
/// One site's access list: its time zone, doors, the access groups that include any of its doors, and the cards of
/// those groups' cardholders. Not thread-safe; callers that read and write concurrently must synchronise.
/// </summary>
public sealed class SiteAccessList
{
    private readonly Dictionary<Guid, DoorEntry> _doors = [];
    private readonly Dictionary<Guid, AccessGroupEntry> _groups = [];
    private readonly Dictionary<Guid, CardEntry> _cardsById = [];
    private readonly Dictionary<string, CardEntry> _cardsByNumber = new(StringComparer.Ordinal);

    public SiteAccessList(string timeZoneId)
    {
        TimeZoneId = timeZoneId;
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    public string TimeZoneId { get; }

    public TimeZoneInfo TimeZone { get; }

    public IReadOnlyCollection<DoorEntry> Doors => _doors.Values;

    public IReadOnlyCollection<AccessGroupEntry> AccessGroups => _groups.Values;

    public IReadOnlyCollection<CardEntry> Cards => _cardsById.Values;

    public CardEntry? FindCard(string cardNumber) => _cardsByNumber.GetValueOrDefault(cardNumber);

    public AccessGroupEntry? FindGroup(Guid accessGroupId) => _groups.GetValueOrDefault(accessGroupId);

    public DoorEntry? FindDoor(Guid doorId) => _doors.GetValueOrDefault(doorId);

    public void Upsert(DoorEntry door) => _doors[door.DoorId] = door;

    public void Upsert(AccessGroupEntry group) => _groups[group.AccessGroupId] = group;

    public void Upsert(CardEntry card)
    {
        if (_cardsById.TryGetValue(card.CardId, out var previous) && previous.CardNumber != card.CardNumber)
        {
            _cardsByNumber.Remove(previous.CardNumber);
        }

        _cardsById[card.CardId] = card;
        _cardsByNumber[card.CardNumber] = card;
    }

    public void Remove(AccessListEntryKind kind, Guid entityId)
    {
        switch (kind)
        {
            case AccessListEntryKind.Door:
                _doors.Remove(entityId);
                break;
            case AccessListEntryKind.AccessGroup:
                _groups.Remove(entityId);
                break;
            case AccessListEntryKind.Card:
                if (_cardsById.Remove(entityId, out var card))
                {
                    _cardsByNumber.Remove(card.CardNumber);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown entry kind.");
        }
    }
}
