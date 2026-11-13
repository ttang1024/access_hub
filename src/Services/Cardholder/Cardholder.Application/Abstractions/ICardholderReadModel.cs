using AccessHub.BuildingBlocks.Application;

namespace AccessHub.Cardholder.Application.Abstractions;

/// <summary>Read-only queries, implemented with no-tracking projections straight to DTOs.</summary>
public interface ICardholderReadModel
{
    Task<SiteDto?> GetSiteAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SiteDto>> ListSitesAsync(PageRequest page, CancellationToken cancellationToken);

    Task<PagedResult<DoorDto>> ListDoorsAsync(Guid siteId, PageRequest page, CancellationToken cancellationToken);

    Task<CardholderDto?> GetCardholderAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CardholderDto>> ListCardholdersAsync(string? search, PageRequest page, CancellationToken cancellationToken);

    Task<CardDto?> GetCardAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CardDto>> ListCardsAsync(Guid cardholderId, PageRequest page, CancellationToken cancellationToken);

    Task<AccessGroupDto?> GetAccessGroupAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AccessGroupDto>> ListAccessGroupsAsync(PageRequest page, CancellationToken cancellationToken);
}
