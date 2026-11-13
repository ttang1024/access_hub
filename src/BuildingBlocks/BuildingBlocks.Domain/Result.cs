namespace AccessHub.BuildingBlocks.Domain;

/// <summary>The outcome of an operation that either succeeds or fails with an expected <see cref="Domain.Error"/>.</summary>
public class Result
{
    /// <summary>Initialises a result.</summary>
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A successful result has no error and a failed result must have one.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The failure, or null on success.</summary>
    public Error? Error { get; }

    /// <summary>A successful result.</summary>
    public static Result Success() => new(true, null);

    /// <summary>A failed result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>A successful result carrying a value.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>A failed result for an operation that would have returned a value.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Converts an error into a failed result.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}
