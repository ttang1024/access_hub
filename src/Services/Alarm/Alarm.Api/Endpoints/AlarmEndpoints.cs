using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Features.Acknowledge;
using AccessHub.Alarm.Application.Features.Close;
using AccessHub.Alarm.Application.Features.Queries;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;

namespace AccessHub.Alarm.Api.Endpoints;

internal static class AlarmEndpoints
{
    public const string Route = "/api/v1/alarms";

    public static IEndpointRouteBuilder MapAlarmEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Alarms");

        group.MapGet("/", async (string? state, int? page, int? pageSize, IQueryHandler<ListAlarmsQuery, PagedResult<AlarmDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListAlarmsQuery(state, page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("List alarms, newest first")
            .WithDescription("state: open (not closed), Raised, Acknowledged or Closed; omit for all.")
            .Produces<PagedResult<AlarmDto>>()
            .ProducesValidationProblem();

        group.MapGet("/{alarmId:guid}", async (Guid alarmId, IQueryHandler<GetAlarmQuery, AlarmDto> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetAlarmQuery(alarmId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("Get an alarm")
            .Produces<AlarmDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{alarmId:guid}/acknowledge", async (Guid alarmId, ICommandHandler<AcknowledgeAlarmCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new AcknowledgeAlarmCommand(alarmId), ct)).Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Operator)
            .WithSummary("Acknowledge a raised alarm")
            .WithDescription("Cancels a pending escalation. Allowed only from Raised; otherwise 409.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{alarmId:guid}/close", async (Guid alarmId, CloseAlarmRequest request, ICommandHandler<CloseAlarmCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new CloseAlarmCommand(alarmId, request.Note ?? string.Empty), ct)).Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Operator)
            .WithSummary("Close an acknowledged alarm with a note")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
