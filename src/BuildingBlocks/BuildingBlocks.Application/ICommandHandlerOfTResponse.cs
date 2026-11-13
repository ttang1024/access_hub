using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Handles a command that returns a value.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The value returned on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
{
    /// <summary>Executes the command.</summary>
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
