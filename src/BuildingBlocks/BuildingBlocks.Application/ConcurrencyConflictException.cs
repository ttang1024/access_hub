namespace AccessHub.BuildingBlocks.Application;

/// <summary>
/// Thrown by the unit of work when an optimistic concurrency check fails or a unique constraint is violated by a
/// concurrent writer. The API maps it to 409, so handlers do not need to know about the persistence technology.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ConcurrencyConflictException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception wrapping the persistence exception.</summary>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
