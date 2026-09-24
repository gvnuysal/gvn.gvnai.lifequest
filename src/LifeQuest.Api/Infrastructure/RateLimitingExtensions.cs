using System.Security.Claims;
using System.Threading.RateLimiting;

namespace LifeQuest.Api.Infrastructure;

internal static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string SuggestionPolicy = "suggestions";

    /// <summary>Hesap ele geçirme / spam denemelerine karşı: auth uçları IP başına, öneri uçları kullanıcı başına.</summary>
    public static IServiceCollection AddLifeQuestRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1) }));

            options.AddPolicy(SuggestionPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
