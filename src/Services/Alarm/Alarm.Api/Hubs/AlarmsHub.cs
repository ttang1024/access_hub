using AccessHub.BuildingBlocks.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AccessHub.Alarm.Api.Hubs;

/// <summary>Server-to-client only: pushes site events and alarm changes. Any operator role may listen.</summary>
[Authorize(Policy = AuthConstants.Policies.Viewer)]
public sealed class AlarmsHub : Hub
{
    public const string Route = "/hubs/alarms";
    public const string SiteEventMethod = "siteEvent";
    public const string AlarmChangedMethod = "alarmChanged";
}
