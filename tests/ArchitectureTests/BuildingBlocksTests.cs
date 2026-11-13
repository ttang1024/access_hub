using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents;
using NetArchTest.Rules;

namespace AccessHub.ArchitectureTests;

public sealed class BuildingBlocksTests
{
    [Fact]
    public void BuildingBlocksDomain_References_NoInfrastructure() =>
        typeof(Result).Assembly.ShouldNotReferenceAny(ForbiddenDependencies.ForDomain);

    [Fact]
    public void BuildingBlocksApplication_References_NoInfrastructure() =>
        typeof(IUnitOfWork).Assembly.ShouldNotReferenceAny(ForbiddenDependencies.ForApplication);

    [Fact]
    public void BuildingBlocksApplication_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(IUnitOfWork).Assembly)
            .ShouldNot().HaveDependencyOn("AccessHub.BuildingBlocks.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void IntegrationEvents_References_OnlyTheBaseLibrary() =>
        typeof(IIntegrationEvent).Assembly.AccessHubReferences().ShouldBeEmpty();

    [Fact]
    public void IntegrationEvents_AreImmutableSealedRecords()
    {
        var eventTypes = typeof(IIntegrationEvent).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(t))
            .ToList();

        eventTypes.ShouldNotBeEmpty();
        foreach (var type in eventTypes)
        {
            type.IsSealed.ShouldBeTrue($"{type.Name} should be sealed");
            type.GetProperties().Where(p => p.SetMethod is { IsPublic: true } && !p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"))
                .ShouldBeEmpty($"{type.Name} should have no public mutable setters");
        }
    }
}
