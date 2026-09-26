using Gvn.GvnFramework.Core.Exceptions;
using Gvn.GvnFramework.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Infrastructure.Persistence;

/// <summary>
/// Framework <c>UnitOfWork&lt;TContext&gt;</c> için decorator (framework <c>Decorate</c> ile sarılır).
/// Optimistic concurrency ve benzersizlik ihlallerini framework <see cref="ConflictException"/>'a çevirir;
/// framework exception middleware'i bunu 409 olarak döner. Böylece Application katmanı EF'e bağımlı olmaz.
/// </summary>
internal sealed class ExceptionTranslatingUnitOfWork(IUnitOfWork inner, ILogger<ExceptionTranslatingUnitOfWork> logger)
    : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await inner.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while saving changes");
            throw new ConflictException(Text.Of("Kayıt eşzamanlı olarak değiştirildi. Lütfen isteği tekrar deneyin.", "The record was changed concurrently. Please try the request again."));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            logger.LogWarning("Unique constraint violation on {Constraint}", pg.ConstraintName);
            throw new ConflictException(Text.Of("Bu işlem zaten gerçekleştirilmiş.", "This action has already been done."));
        }
    }
}
