using AccessHub.BuildingBlocks.Domain;
using FluentValidation;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Decorator that validates a command before the inner handler runs.</summary>
public sealed class ValidatingCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var error = await ValidationErrors.ValidateAsync(validators, command, cancellationToken);
        return error is null ? await inner.HandleAsync(command, cancellationToken) : Result.Failure(error);
    }
}
