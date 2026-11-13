using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Registers handlers and validators from an assembly, wrapping every handler in its validation decorator.</summary>
public static class HandlerRegistration
{
    private static readonly Dictionary<Type, Type> Decorators = new()
    {
        [typeof(ICommandHandler<>)] = typeof(ValidatingCommandHandler<>),
        [typeof(ICommandHandler<,>)] = typeof(ValidatingCommandHandler<,>),
        [typeof(IQueryHandler<,>)] = typeof(ValidatingQueryHandler<,>),
    };

    /// <summary>
    /// Registers every concrete handler in <paramref name="assembly"/> as scoped, resolved through its validation decorator,
    /// and every FluentValidation validator.
    /// </summary>
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        var handlerTypes = assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });
        foreach (var implementation in handlerTypes)
        {
            foreach (var serviceType in implementation.GetInterfaces().Where(i => i.IsGenericType && Decorators.ContainsKey(i.GetGenericTypeDefinition())))
            {
                services.AddScoped(implementation);
                var decorator = Decorators[serviceType.GetGenericTypeDefinition()].MakeGenericType(serviceType.GetGenericArguments());
                services.AddScoped(serviceType, sp => ActivatorUtilities.CreateInstance(sp, decorator, sp.GetRequiredService(implementation)));
            }
        }

        return services;
    }
}
