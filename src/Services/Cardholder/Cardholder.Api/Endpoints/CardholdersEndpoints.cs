using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.Cardholders;
using AccessHub.Cardholder.Application.Features.Cards;

namespace AccessHub.Cardholder.Api.Endpoints;

internal static class CardholdersEndpoints
{
    public static IEndpointRouteBuilder MapCardholdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Cardholders).WithTags(ApiRoutes.Tags.Cardholders);

        group.MapPost("/", async (CreateCardholderRequest request, ICommandHandler<CreateCardholderCommand, Guid> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new CreateCardholderCommand(request.FirstName, request.LastName, request.AccessGroupIds), ct))
                    .Match(id => Results.Created($"{ApiRoutes.Cardholders}/{id}", new IdResponse(id))))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Create a cardholder")
            .Produces<IdResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{cardholderId:guid}", async (Guid cardholderId, UpdateCardholderRequest request, ICommandHandler<UpdateCardholderCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(
                    new UpdateCardholderCommand(cardholderId, request.FirstName, request.LastName, request.IsActive, request.AccessGroupIds, request.ExpectedVersion), ct))
                    .Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Update a cardholder (name, active flag, access groups)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (string? search, int? page, int? pageSize, IQueryHandler<ListCardholdersQuery, PagedResult<CardholderDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListCardholdersQuery(search, page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("List cardholders")
            .Produces<PagedResult<CardholderDto>>();

        group.MapGet("/{cardholderId:guid}", async (Guid cardholderId, IQueryHandler<GetCardholderQuery, CardholderDto> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetCardholderQuery(cardholderId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("Get a cardholder")
            .Produces<CardholderDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{cardholderId:guid}/cards", async (Guid cardholderId, IssueCardRequest request, ICommandHandler<IssueCardCommand, Guid> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new IssueCardCommand(cardholderId, request.CardNumber, request.ExpiresAt), ct))
                    .Match(id => Results.Created($"{ApiRoutes.Cards}/{id}", new IdResponse(id))))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithTags(ApiRoutes.Tags.Cards)
            .WithSummary("Issue a card to a cardholder")
            .Produces<IdResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{cardholderId:guid}/cards", async (Guid cardholderId, int? page, int? pageSize, IQueryHandler<ListCardsQuery, PagedResult<CardDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListCardsQuery(cardholderId, page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithTags(ApiRoutes.Tags.Cards)
            .WithSummary("List a cardholder's cards")
            .Produces<PagedResult<CardDto>>();

        return app;
    }
}
