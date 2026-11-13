using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Handles a read-only query.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The value returned on success.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
{
    /// <summary>Runs the query.</summary>
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
