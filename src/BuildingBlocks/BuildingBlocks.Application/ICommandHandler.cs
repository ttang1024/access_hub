using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Handles a command that returns no value.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand>
{
    /// <summary>Executes the command.</summary>
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
