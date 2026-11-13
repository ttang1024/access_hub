using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.AccessGroups;

/// <summary>A set of doors, each with a weekly schedule. Cardholders in the group may open those doors in those windows.</summary>
public sealed class AccessGroup : AggregateRoot<AccessGroupId>
{
    public const int MaxNameLength = 200;
    public const int MaxDoors = 500;

    private IReadOnlyList<DoorAccess> _doors = [];

    private AccessGroup()
    {
    }

    private AccessGroup(AccessGroupId id, string name)
        : base(id) => Name = name;

    public string Name { get; private set; } = string.Empty;

    public bool IsDeleted { get; private set; }

    public IReadOnlyList<DoorAccess> Doors => _doors;

    public static Result<AccessGroup> Create(string name, IReadOnlyList<DoorAccess> doors, DateTimeOffset now)
    {
        var validation = Validate(name, doors);
        if (validation.IsFailure)
        {
            return validation.Error!;
        }

        var group = new AccessGroup(AccessGroupId.New(), name.Trim()) { _doors = doors.ToList() };
        group.RaiseChanged(now);
        return group;
    }

    public Result Update(string name, IReadOnlyList<DoorAccess> doors, int? expectedVersion, DateTimeOffset now)
    {
        var guard = GuardChange(expectedVersion);
        if (guard.IsFailure)
        {
            return guard;
        }

        var validation = Validate(name, doors);
        if (validation.IsFailure)
        {
            return validation;
        }

        if (Name == name.Trim() && _doors.SequenceEqual(doors))
        {
            return Result.Success();
        }

        Name = name.Trim();
        _doors = doors.ToList();
        RaiseChanged(now);
        return Result.Success();
    }

    /// <summary>Soft-deletes the group. The tombstone event tells consumers to drop it from every site list.</summary>
    public Result Delete(int? expectedVersion, DateTimeOffset now)
    {
        var guard = GuardChange(expectedVersion);
        if (guard.IsFailure)
        {
            return guard;
        }

        IsDeleted = true;
        _doors = [];
        RaiseChanged(now);
        return Result.Success();
    }

    private static Result Validate(string name, IReadOnlyList<DoorAccess> doors)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            return AccessGroupErrors.InvalidName;
        }

        if (doors.Count > MaxDoors)
        {
            return AccessGroupErrors.TooManyDoors;
        }

        return doors.Select(d => d.DoorId).Distinct().Count() != doors.Count
            ? AccessGroupErrors.DuplicateDoor
            : Result.Success();
    }

    private Result GuardChange(int? expectedVersion)
    {
        if (IsDeleted)
        {
            return AccessGroupErrors.Deleted(Id);
        }

        return expectedVersion is { } expected && expected != Version
            ? AccessGroupErrors.VersionMismatch(expected, Version)
            : Result.Success();
    }

    private void RaiseChanged(DateTimeOffset now) =>
        Apply(v => new AccessGroupChangedDomainEvent(Id, Name, IsDeleted, _doors.ToList(), v, now));
}
