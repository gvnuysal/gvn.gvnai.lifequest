using Gvn.GvnFramework.Core.Exceptions;
using Gvn.GvnFramework.Security.Abstractions;
using LifeQuest.Application.Abstractions;

namespace LifeQuest.Api.Infrastructure;

/// <summary>Framework <see cref="ICurrentUserService"/> üzerinden token'daki kullanıcıyı Guid olarak çözer.</summary>
internal sealed class HttpUserContext(ICurrentUserService currentUser) : IUserContext
{
    public bool IsAuthenticated => currentUser.IsAuthenticated;

    public Guid UserId =>
        currentUser.IsAuthenticated && Guid.TryParse(currentUser.UserId, out var id)
            ? id
            : throw new UnauthorizedException();
}
