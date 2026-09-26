using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Identity;

namespace LifeQuest.Application.Identity;

public sealed record LogoutCommand(string? RefreshToken) : ICommand;

internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserContext user,
    TimeProvider clock) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return Result.Ok();

        var token = await refreshTokens.GetByHashAsync(AuthTokenIssuer.Hash(command.RefreshToken), cancellationToken);

        // Başkasına ait ya da bilinmeyen token için de aynı yanıt: bilgi sızdırılmaz.
        if (token is not null && token.UserId == user.UserId)
        {
            await refreshTokens.RevokeFamilyAsync(token.FamilyId, clock.GetUtcNow().UtcDateTime, "logout", cancellationToken);
        }

        return Result.Ok();
    }
}
