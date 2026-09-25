using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.Admin;

public interface IAdminAuditRepository : IRepository<AdminAuditEntry>
{
    Task<(IReadOnlyList<AdminAuditEntry> Items, int TotalCount)> GetPageAsync(
        AdminAction? action, AdminTargetType? targetType, int skip, int take, CancellationToken cancellationToken = default);
}
