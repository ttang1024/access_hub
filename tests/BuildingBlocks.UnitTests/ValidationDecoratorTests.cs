using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.BuildingBlocks.UnitTests;

public sealed class ValidationDecoratorTests
{
    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsValidationErrorWithoutCallingHandler()
    {
        var handler = BuildHandler();

        var result = await handler.HandleAsync(new Greet(""), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.FieldErrors.ShouldContainKey(nameof(Greet.Name));
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CallsHandler()
    {
        var handler = BuildHandler();

        var result = await handler.HandleAsync(new Greet("Ada"), TestContext.Current.CancellationToken);

        result.Value.ShouldBe("Hello Ada");
    }

    private static ICommandHandler<Greet, string> BuildHandler()
    {
        var services = new ServiceCollection();
        services.AddApplicationHandlers(typeof(ValidationDecoratorTests).Assembly);
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<ICommandHandler<Greet, string>>();
    }

    public sealed record Greet(string Name);

    public sealed class GreetValidator : AbstractValidator<Greet>
    {
        public GreetValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    public sealed class GreetHandler : ICommandHandler<Greet, string>
    {
        public Task<Result<string>> HandleAsync(Greet command, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>($"Hello {command.Name}");
    }
}
