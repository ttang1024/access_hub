namespace AccessHub.BuildingBlocks.Domain;

/// <summary>The outcome of an operation that returns a value on success.</summary>
/// <typeparam name="T">The value type.</typeparam>
#pragma warning disable CA1000 // Static factories on the generic type are the intended API (Result<Card>.Success(card)).
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error) => _value = value;

    /// <summary>The value. Throws if the result is a failure, because reading it then is a bug.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error!.Code}).");

    /// <summary>A successful result.</summary>
    public static Result<T> Success(T value) => new(value, true, null);

    /// <summary>A failed result.</summary>
    public static new Result<T> Failure(Error error) => new(default, false, error);

    /// <summary>Converts a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Converts an error into a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
#pragma warning restore CA1000
