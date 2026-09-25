using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Quests;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LifeQuest.Infrastructure.Admin;

/// <summary>
/// Admin kullanıcı listesi: hesap alanları ve SQL tarafında hesaplanan iki toplam (tamamlanan görev, Life XP).
/// Görev içeriği, puan veya ilgi alanı okunmaz.
/// </summary>
internal sealed class AdminUserReader(LifeQuestDbContext db) : IAdminUserReader
{
    public async Task<(IReadOnlyList<AdminUserRow> Items, int TotalCount)> SearchAsync(
        string? search, AdminUserFilter filter, DateTime nowUtc, int skip, int take, CancellationToken cancellationToken = default)
    {
        var accounts = db.UserAccounts.AsNoTracking();
        if (search is not null)
        {
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            accounts = accounts.Where(a => EF.Functions.ILike(a.Email, pattern, "\\") || EF.Functions.ILike(a.DisplayName, pattern, "\\"));
        }

        accounts = filter switch
        {
            AdminUserFilter.Admins => accounts.Where(a => a.Role == UserRoles.Admin),
            AdminUserFilter.Suspended => accounts.Where(a => a.SuspendedAt != null && (a.SuspendedUntil == null || a.SuspendedUntil > nowUtc)),
            _ => accounts
        };

        var total = await accounts.CountAsync(cancellationToken);
        var items = await Project(accounts.OrderByDescending(a => a.CreatedAt).Skip(skip).Take(take)).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<AdminUserRow?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => Project(db.UserAccounts.AsNoTracking().Where(a => a.Id == userId)).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<AdminUserRow> Project(IQueryable<UserAccount> accounts)
        => accounts.Select(a => new AdminUserRow(
            a.Id, a.Email, a.DisplayName, a.Role, a.CreatedAt, a.LastLoginAt,
            a.SuspendedAt, a.SuspendedUntil, a.SuspensionReason,
            db.UserQuests.Count(q => q.UserId == a.Id && q.Status == QuestStatus.Completed),
            db.PlayerProgress.Where(p => p.UserId == a.Id).Select(p => p.LifeXp).FirstOrDefault()));
}
