using Gvn.GvnFramework.EntityFrameworkCore.Repositories;
using LifeQuest.Domain.RealWorld;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Persistence.Repositories;

internal sealed class LocalPlaceRepository(LifeQuestDbContext context)
    : EfRepository<LocalPlace, LifeQuestDbContext>(context), ILocalPlaceRepository
{
    public async Task<IReadOnlyList<LocalPlace>> GetAllAsync(string? cityKey, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        if (cityKey is not null)
            query = query.Where(p => p.CityKey == cityKey);
        return await query
            .OrderBy(p => p.City).ThenBy(p => p.Kind).ThenBy(p => p.StartsAt).ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetEventTemplateIdsAsync(
        string cityKey, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        => (await DbSet.AsNoTracking()
                .Where(p => p.CityKey == cityKey && p.IsActive && p.Kind == LocalPlaceKind.Event &&
                            p.StartsAt <= toUtc && p.EndsAt >= fromUtc)
                .Select(p => p.TemplateIds)
                .ToListAsync(cancellationToken))
            .SelectMany(ids => ids)
            .ToHashSet();

    public async Task<IReadOnlyList<LocalPlace>> GetForTemplateAsync(
        string cityKey, Guid templateId, DateTime nowUtc, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(p => p.CityKey == cityKey && p.IsActive && p.TemplateIds.Contains(templateId) &&
                        (p.Kind == LocalPlaceKind.Venue || p.EndsAt >= nowUtc))
            .OrderBy(p => p.Kind == LocalPlaceKind.Event ? 0 : 1).ThenBy(p => p.StartsAt).ThenBy(p => p.Name)
            .Take(5)
            .ToListAsync(cancellationToken);
}
