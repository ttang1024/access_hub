namespace AccessHub.BuildingBlocks.Domain;

/// <summary>
/// Marker for value objects. Value objects are implemented as C# records, which already give structural equality;
/// the marker documents intent and lets architecture tests find them.
/// </summary>
public interface IValueObject;
