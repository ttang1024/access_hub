namespace AccessHub.BuildingBlocks.Domain;

/// <summary>Something that happened inside an aggregate. Dispatched by the unit of work before commit.</summary>
public interface IDomainEvent
{
    /// <summary>When the change happened, in UTC.</summary>
    DateTimeOffset OccurredAt { get; }
}
