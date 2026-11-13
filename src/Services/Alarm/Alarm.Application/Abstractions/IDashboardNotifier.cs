namespace AccessHub.Alarm.Application.Abstractions;

/// <summary>Pushes live updates to connected dashboards.</summary>
public interface IDashboardNotifier
{
    Task SiteEventAsync(SiteEventDto siteEvent, CancellationToken cancellationToken);

    Task AlarmChangedAsync(AlarmDto alarm, CancellationToken cancellationToken);
}
