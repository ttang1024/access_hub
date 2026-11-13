namespace AccessHub.Alarm.Application.Features.Queries;

public sealed record ListAlarmsQuery(string? State, int Page = 1, int PageSize = 20);
