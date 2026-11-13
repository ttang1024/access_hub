using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Domain.Cardholders;

/// <summary>A person who holds cards and belongs to access groups.</summary>
public sealed class Cardholder : AggregateRoot<CardholderId>
{
    public const int MaxNameLength = 100;
    public const int MaxGroups = 50;

    private IReadOnlyList<AccessGroupId> _accessGroupIds = [];

    private Cardholder()
    {
    }

    private Cardholder(CardholderId id, string firstName, string lastName)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
    }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public IReadOnlyList<AccessGroupId> AccessGroupIds => _accessGroupIds;

    public static Result<Cardholder> Create(string firstName, string lastName, IReadOnlyCollection<AccessGroupId> accessGroupIds, DateTimeOffset now)
    {
        if (!ValidName(firstName) || !ValidName(lastName))
        {
            return CardholderErrors.InvalidName;
        }

        if (accessGroupIds.Count > MaxGroups)
        {
            return CardholderErrors.TooManyGroups;
        }

        var cardholder = new Cardholder(CardholderId.New(), firstName.Trim(), lastName.Trim())
        {
            _accessGroupIds = accessGroupIds.Distinct().ToList(),
        };
        cardholder.RaiseChanged(now);
        return cardholder;
    }

    /// <summary>Replaces the editable state. Raises one event only if something actually changed.</summary>
    public Result Update(string firstName, string lastName, bool isActive, IReadOnlyCollection<AccessGroupId> accessGroupIds, int? expectedVersion, DateTimeOffset now)
    {
        if (expectedVersion is { } expected && expected != Version)
        {
            return CardholderErrors.VersionMismatch(expected, Version);
        }

        if (!ValidName(firstName) || !ValidName(lastName))
        {
            return CardholderErrors.InvalidName;
        }

        if (accessGroupIds.Count > MaxGroups)
        {
            return CardholderErrors.TooManyGroups;
        }

        var groups = accessGroupIds.Distinct().ToList();
        var unchanged = FirstName == firstName.Trim() && LastName == lastName.Trim() && IsActive == isActive
            && groups.Count == _accessGroupIds.Count && groups.All(g => _accessGroupIds.Contains(g));
        if (unchanged)
        {
            return Result.Success();
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        IsActive = isActive;
        _accessGroupIds = groups;
        RaiseChanged(now);
        return Result.Success();
    }

    private static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= MaxNameLength;

    private void RaiseChanged(DateTimeOffset now) =>
        Apply(v => new CardholderChangedDomainEvent(Id, FirstName, LastName, IsActive, _accessGroupIds.ToList(), v, now));
}
