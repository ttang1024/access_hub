using System.Reflection;

namespace AccessHub.ArchitectureTests;

/// <summary>The services under test. Each has Domain, Application, Infrastructure and Api projects.</summary>
internal static class Services
{
    public static readonly string[] All = ["Cardholder", "AccessControl", "EventIngestion", "Alarm"];

    public static Assembly Layer(string service, string layer) => Assembly.Load($"{service}.{layer}");

    public static TheoryData<string> Names()
    {
        var data = new TheoryData<string>();
        foreach (var service in All)
        {
            data.Add(service);
        }

        return data;
    }
}
