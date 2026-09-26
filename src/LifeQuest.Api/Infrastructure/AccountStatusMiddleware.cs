using System.Security.Claims;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Api.Infrastructure;

/// <summary>
/// Access token 15 dk geçerlidir ve rolü taşır. Admin bir hesabı askıya aldığında veya rolünü değiştirdiğinde
/// bunun token süresini beklemeden etkili olması için her kimlikli istekte hesabın güncel durumu (kısa ömürlü
/// cache) kontrol edilir. Rol uyuşmazlığında 401 TOKEN_STALE döner; istemci refresh ile güncel rolü alır.
/// </summary>
internal sealed class AccountStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserContext user, IAccountStateCache accountState, TimeProvider clock)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var state = await accountState.GetAsync(user.UserId, context.RequestAborted);
        if (state is null || state.IsSuspended(clock.GetUtcNow().UtcDateTime))
        {
            await RejectAsync(context, "ACCOUNT_SUSPENDED", Text.Of("Hesabın askıya alındı veya artık mevcut değil.", "Your account has been suspended or no longer exists."));
            return;
        }

        var tokenRole = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role");
        if (!string.Equals(tokenRole, state.Role, StringComparison.Ordinal))
        {
            await RejectAsync(context, "TOKEN_STALE", Text.Of("Hesap yetkilerin değişti; oturum yenileniyor.", "Your account permissions changed; refreshing the session."));
            return;
        }

        await next(context);
    }

    private static Task RejectAsync(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new[] { new { code, message, type = "Unauthorized" } });
    }
}
