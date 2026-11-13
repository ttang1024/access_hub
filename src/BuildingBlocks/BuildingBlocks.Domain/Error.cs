namespace AccessHub.BuildingBlocks.Domain;

/// <summary>A typed, expected failure. Exceptions are reserved for bugs and infrastructure failure.</summary>
/// <param name="Code">Stable machine-readable code, for example <c>Card.NotFound</c>.</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Type">Category used to map the error to a status code.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Field-level validation messages, keyed by field name. Empty unless <see cref="Type"/> is <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    /// <inheritdoc />
    public bool Equals(Error? other) =>
        other is not null && Code == other.Code && Message == other.Message && Type == other.Type
        && FieldErrors.Count == other.FieldErrors.Count
        && FieldErrors.All(kv => other.FieldErrors.TryGetValue(kv.Key, out var v) && v.SequenceEqual(kv.Value));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Code, Message, Type, FieldErrors.Count);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates a forbidden error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates a domain rule failure.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>Creates a single-field validation error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a validation error with per-field messages.</summary>
    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation) { FieldErrors = fieldErrors };
}
