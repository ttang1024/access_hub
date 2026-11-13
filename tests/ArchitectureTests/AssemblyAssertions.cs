using System.Reflection;

namespace AccessHub.ArchitectureTests;

internal static class AssemblyAssertions
{
    public static void ShouldNotReferenceAny(this Assembly assembly, IEnumerable<string> forbiddenPrefixes)
    {
        var offending = assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        offending.ShouldBeEmpty($"{assembly.GetName().Name} must not reference: {string.Join(", ", offending)}");
    }

    public static IReadOnlyList<string> AccessHubReferences(this Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("BuildingBlocks.", StringComparison.Ordinal) || n.StartsWith("Contracts.", StringComparison.Ordinal)
                || Services.All.Any(s => n.StartsWith(s + ".", StringComparison.Ordinal))
                || n is "ServiceDefaults" or "SiteSimulator")
            .ToList();
}
