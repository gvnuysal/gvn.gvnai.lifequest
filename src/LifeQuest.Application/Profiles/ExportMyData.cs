using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Identity;

namespace LifeQuest.Application.Profiles;

/// <summary>KVKK/GDPR veri taşınabilirliği: kullanıcının tüm verisini makinece okunabilir JSON olarak üretir.</summary>
public interface IUserDataExporter
{
    /// <returns>Hesap yoksa <c>null</c>.</returns>
    Task<byte[]?> ExportAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken = default);
}

public sealed record DataExportFile(string FileName, byte[] Content);

public sealed record ExportMyDataQuery : IQuery<DataExportFile>;

internal sealed class ExportMyDataQueryHandler(IUserDataExporter exporter, IUserContext user, TimeProvider clock)
    : IQueryHandler<ExportMyDataQuery, DataExportFile>
{
    public async Task<Result<DataExportFile>> Handle(ExportMyDataQuery query, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var content = await exporter.ExportAsync(user.UserId, now, cancellationToken);
        return content is null
            ? Result<DataExportFile>.Fail(IdentityErrors.AccountNotFound)
            : Result<DataExportFile>.Ok(new DataExportFile($"lifequest-verilerim-{now:yyyyMMdd}.json", content));
    }
}
