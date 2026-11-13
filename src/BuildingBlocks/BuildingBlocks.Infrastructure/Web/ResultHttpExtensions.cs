using AccessHub.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Http;

namespace AccessHub.BuildingBlocks.Infrastructure.Web;

/// <summary>Maps <see cref="Result"/> failures to RFC 9457 ProblemDetails responses.</summary>
public static class ResultHttpExtensions
{
    /// <summary>The HTTP status for an error type.</summary>
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Builds a ProblemDetails (or ValidationProblemDetails) response for the error.</summary>
    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        return error.Type == ErrorType.Validation && error.FieldErrors.Count > 0
            ? Results.ValidationProblem(
                error.FieldErrors.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal),
                detail: error.Message,
                extensions: extensions)
            : Results.Problem(detail: error.Message, statusCode: error.Type.ToStatusCode(), title: error.Code, extensions: extensions);
    }

    /// <summary>Returns <paramref name="onSuccess"/> or the problem response.</summary>
    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : result.Error!.ToProblem();

    /// <summary>Returns <paramref name="onSuccess"/> applied to the value, or the problem response.</summary>
    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error!.ToProblem();
}
