using Gvn.GvnFramework.AspNetCore.Controllers;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Api.Infrastructure;
using LifeQuest.Application.Identity;
using LifeQuest.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeQuest.Api.Controllers;

[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AuthController(ISender sender, RefreshTokenCookie cookie) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
        => Session(await sender.Send(command, cancellationToken));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
        => Session(await sender.Send(command, cancellationToken));

    /// <summary>
    /// Web: refresh token çerezden okunur; gövdedeki token yalnızca eski sürümün localStorage'daki token'ını bir kez
    /// çereze taşımak içindir. Native istemci (<see cref="NativeClientHeader"/>): token yalnızca gövdeden okunur ve yenisi
    /// gövdede döner; çerez ne okunur ne yazılır. Böylece web'deki bir betik başlığı ekleyerek çerezdeki token'ı
    /// gövdeye çıkaramaz.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var token = IsNativeClient ? request?.RefreshToken : cookie.Read(Request) ?? request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(token))
            return HandleResult(Result<AuthTokensDto>.Fail(IdentityErrors.InvalidRefreshToken));

        var result = await sender.Send(new RefreshTokenCommand(token), cancellationToken);
        if (!result.Succeeded && !IsNativeClient)
            cookie.Clear(Response);
        return Session(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(cookie.Read(Request) ?? request?.RefreshToken), cancellationToken);
        cookie.Clear(Response);
        return HandleResult(result);
    }

    private IActionResult Session(Result<AuthTokensDto> result)
    {
        if (!result.Succeeded)
            return HandleResult(result);

        if (IsNativeClient)
            return Ok(AuthSessionResponse.ForNativeClient(result.Data!));

        cookie.Write(Response, result.Data!);
        return Ok(AuthSessionResponse.From(result.Data!));
    }

    /// <summary>Mobil uygulama bu başlıkla refresh token'ı gövdede alır ve cihazın güvenli deposunda saklar.</summary>
    public const string NativeClientHeader = "X-LifeQuest-Client";

    private bool IsNativeClient
        => string.Equals(Request.Headers[NativeClientHeader], "native", StringComparison.OrdinalIgnoreCase);

    public sealed record RefreshRequest(string? RefreshToken);
}
