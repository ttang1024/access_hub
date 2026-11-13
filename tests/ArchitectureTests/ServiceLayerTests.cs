using AccessHub.BuildingBlocks.Domain;
using NetArchTest.Rules;

namespace AccessHub.ArchitectureTests;

/// <summary>Clean Architecture rules, checked for every service.</summary>
public sealed class ServiceLayerTests
{
    public static TheoryData<string> ServiceNames => Services.Names();

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Domain_References_NoInfrastructurePackage(string service) =>
        Services.Layer(service, "Domain").ShouldNotReferenceAny(ForbiddenDependencies.ForDomain);

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Domain_DependsOn_NoOuterLayer(string service)
    {
        var result = Types.InAssembly(Services.Layer(service, "Domain"))
            .ShouldNot().HaveDependencyOnAny($"AccessHub.{service}.Application", $"AccessHub.{service}.Infrastructure", $"AccessHub.{service}.Api", "AccessHub.Contracts")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failing(result));
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Application_References_NoInfrastructurePackage(string service) =>
        Services.Layer(service, "Application").ShouldNotReferenceAny(ForbiddenDependencies.ForApplication);

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Application_DependsOn_NeitherInfrastructureNorApi(string service)
    {
        var result = Types.InAssembly(Services.Layer(service, "Application"))
            .ShouldNot().HaveDependencyOnAny($"AccessHub.{service}.Infrastructure", $"AccessHub.{service}.Api", "AccessHub.BuildingBlocks.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failing(result));
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Infrastructure_DependsOn_NotApi(string service)
    {
        var result = Types.InAssembly(Services.Layer(service, "Infrastructure"))
            .ShouldNot().HaveDependencyOn($"AccessHub.{service}.Api")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Failing(result));
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void ServiceProjects_Reference_NoOtherService(string service)
    {
        var others = Services.All.Where(s => s != service).Select(s => s + ".").ToArray();
        foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
        {
            Services.Layer(service, layer).ShouldNotReferenceAny(others);
        }
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void DomainTypes_Have_NoPublicMutableSetters(string service)
    {
        var mutable = Services.Layer(service, "Domain").GetTypes()
            .Where(t => t is { IsClass: true, IsPublic: true })
            .SelectMany(t => t.GetProperties().Where(p => p.SetMethod?.IsPublic == true && !IsInitOnly(p)).Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        mutable.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Aggregates_AreCreated_ThroughFactoryMethods(string service)
    {
        var aggregates = Services.Layer(service, "Domain").GetTypes()
            .Where(t => typeof(IAggregateRoot).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var type in aggregates)
        {
            type.GetConstructors().Where(c => c.IsPublic).ShouldBeEmpty($"{type.Name} should be created through a factory method");
        }
    }

    private static bool IsInitOnly(System.Reflection.PropertyInfo property) =>
        property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static string Failing(NetArchTest.Rules.TestResult result) => string.Join(", ", result.FailingTypeNames ?? []);
}
