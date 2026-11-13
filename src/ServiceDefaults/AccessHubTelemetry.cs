namespace AccessHub.ServiceDefaults;

/// <summary>Names of the AccessHub meters and activity sources, registered with OpenTelemetry.</summary>
public static class AccessHubTelemetry
{
    /// <summary>Meter and activity source name shared by all AccessHub components.</summary>
    public const string Name = "AccessHub";

    /// <summary>MassTransit's diagnostic source name (trace context flows through message headers).</summary>
    public const string MassTransitSource = "MassTransit";
}
