using AccessHub.BuildingBlocks.Domain;
using FluentValidation;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Runs FluentValidation validators and turns failures into a validation <see cref="Error"/>.</summary>
public static class ValidationErrors
{
    /// <summary>Validates <paramref name="instance"/> with every registered validator; returns null when valid.</summary>
    public static async Task<Error?> ValidateAsync<T>(
        IEnumerable<IValidator<T>> validators, T instance, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(instance, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return null;
        }

        var fields = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        return Error.Validation(fields);
    }
}
