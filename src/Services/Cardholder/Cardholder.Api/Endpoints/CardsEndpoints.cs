using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.Cards;

namespace AccessHub.Cardholder.Api.Endpoints;

internal static class CardsEndpoints
{
    public static IEndpointRouteBuilder MapCardsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Cards).WithTags(ApiRoutes.Tags.Cards);

        group.MapGet("/{cardId:guid}", async (Guid cardId, IQueryHandler<GetCardQuery, CardDto> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetCardQuery(cardId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("Get a card")
            .Produces<CardDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{cardId:guid}/revoke", async (Guid cardId, ICommandHandler<RevokeCardCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new RevokeCardCommand(cardId), ct)).Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Revoke a card")
            .WithDescription("Revocation is permanent and reaches every connected site within seconds.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
