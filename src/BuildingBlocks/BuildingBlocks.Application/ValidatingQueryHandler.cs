using AccessHub.BuildingBlocks.Domain;
using FluentValidation;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Decorator that validates a query before the inner handler runs.</summary>
public sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
{
    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var error = await ValidationErrors.ValidateAsync(validators, query, cancellationToken);
        return error is null ? await inner.HandleAsync(query, cancellationToken) : Result<TResponse>.Failure(error);
    }
}
