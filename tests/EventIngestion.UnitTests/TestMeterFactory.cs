using System.Diagnostics.Metrics;

namespace AccessHub.EventIngestion.UnitTests;

internal sealed class TestMeterFactory : IMeterFactory
{
    public Meter Create(MeterOptions options) => new(options);

    public void Dispose()
    {
    }
}
