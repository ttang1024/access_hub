using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;
using AccessHub.Cardholder.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.Infrastructure.Persistence;

internal sealed class CardholderReadModel(CardholderDbContext db) : ICardholderReadModel
{
    public Task<SiteDto?> GetSiteAsync(Guid id, CancellationToken cancellationToken) =>
        db.Sites.AsNoTracking().Where(s => s.Id == new SiteId(id))
            .Select(s => new SiteDto(s.Id.Value, s.Name, s.TimeZoneId, s.CreatedAt, s.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PagedResult<SiteDto>> ListSitesAsync(PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            db.Sites.AsNoTracking().OrderBy(s => s.Name).ThenBy(s => s.Id)
                .Select(s => new SiteDto(s.Id.Value, s.Name, s.TimeZoneId, s.CreatedAt, s.Version)),
            page,
            cancellationToken);

    public Task<PagedResult<DoorDto>> ListDoorsAsync(Guid siteId, PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            db.Doors.AsNoTracking().Where(d => d.SiteId == new SiteId(siteId)).OrderBy(d => d.Name).ThenBy(d => d.Id)
                .Select(d => new DoorDto(d.Id.Value, d.SiteId.Value, d.Name, d.Version)),
            page,
            cancellationToken);

    public async Task<CardholderDto?> GetCardholderAsync(Guid id, CancellationToken cancellationToken)
    {
        var c = await db.Cardholders.AsNoTracking().SingleOrDefaultAsync(c => c.Id == new CardholderId(id), cancellationToken);
        return c is null ? null : new CardholderDto(c.Id.Value, c.FirstName, c.LastName, c.IsActive, c.AccessGroupIds.Select(g => g.Value).ToList(), c.Version);
    }

    public async Task<PagedResult<CardholderDto>> ListCardholdersAsync(string? search, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Cardholders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
            query = query.Where(c => EF.Functions.ILike(c.LastName, pattern) || EF.Functions.ILike(c.FirstName, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
            .Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedResult<CardholderDto>(
            items.Select(c => new CardholderDto(c.Id.Value, c.FirstName, c.LastName, c.IsActive, c.AccessGroupIds.Select(g => g.Value).ToList(), c.Version)).ToList(),
            page.Page,
            page.Take,
            total);
    }

    public Task<CardDto?> GetCardAsync(Guid id, CancellationToken cancellationToken) =>
        db.Cards.AsNoTracking().Where(c => c.Id == new CardId(id))
            .Select(c => new CardDto(c.Id.Value, c.CardholderId.Value, c.Number, c.IssuedAt, c.ExpiresAt, c.RevokedAt, c.RevokedAt != null, c.Version))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PagedResult<CardDto>> ListCardsAsync(Guid cardholderId, PageRequest page, CancellationToken cancellationToken) =>
        PageAsync(
            db.Cards.AsNoTracking().Where(c => c.CardholderId == new CardholderId(cardholderId)).OrderBy(c => c.IssuedAt).ThenBy(c => c.Id)
                .Select(c => new CardDto(c.Id.Value, c.CardholderId.Value, c.Number, c.IssuedAt, c.ExpiresAt, c.RevokedAt, c.RevokedAt != null, c.Version)),
            page,
            cancellationToken);

    public async Task<AccessGroupDto?> GetAccessGroupAsync(Guid id, CancellationToken cancellationToken)
    {
        // Doors are a jsonb document, so the group is materialised and mapped in memory.
        var group = await db.AccessGroups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == new AccessGroupId(id) && !g.IsDeleted, cancellationToken);
        return group is null ? null : ToDto(group);
    }

    public async Task<PagedResult<AccessGroupDto>> ListAccessGroupsAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.AccessGroups.AsNoTracking().Where(g => !g.IsDeleted);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(g => g.Name).ThenBy(g => g.Id).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedResult<AccessGroupDto>(items.Select(ToDto).ToList(), page.Page, page.Take, total);
    }

    private static AccessGroupDto ToDto(AccessGroup g) => new(
        g.Id.Value,
        g.Name,
        g.Doors.Select(d => new AccessGroupDoorDto(d.DoorId.Value, d.SiteId.Value, d.Schedule.Windows.Select(w => new TimeWindowDto(w.Day, w.StartMinute, w.EndMinute)).ToList())).ToList(),
        g.Version);

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.Page, page.Take, total);
    }
}
