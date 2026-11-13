using AccessHub.Alarm.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace AccessHub.Alarm.Api.Hubs;

/// <summary>With the Redis backplane, a message sent by any replica reaches dashboards connected to every replica.</summary>
internal sealed class SignalRDashboardNotifier(IHubContext<AlarmsHub> hub) : IDashboardNotifier
{
    public Task SiteEventAsync(SiteEventDto siteEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(AlarmsHub.SiteEventMethod, siteEvent, cancellationToken);

    public Task AlarmChangedAsync(AlarmDto alarm, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(AlarmsHub.AlarmChangedMethod, alarm, cancellationToken);
}
