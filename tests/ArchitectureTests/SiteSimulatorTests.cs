using System.Reflection;

namespace AccessHub.ArchitectureTests;

/// <summary>The site controller is the edge deployment of AccessControl: it shares only the decision rules and the contracts.</summary>
public sealed class SiteSimulatorTests
{
    private static readonly string[] Allowed = ["AccessControl.Domain", "Contracts.Grpc", "Contracts.IntegrationEvents", "ServiceDefaults", "BuildingBlocks.Domain"];

    [Fact]
    public void SiteSimulator_References_OnlyAccessControlDomainContractsAndServiceDefaults()
    {
        var references = Assembly.Load("SiteSimulator").AccessHubReferences();

        references.ShouldContain("AccessControl.Domain");
        references.Where(r => !Allowed.Contains(r)).ShouldBeEmpty();
    }
}
