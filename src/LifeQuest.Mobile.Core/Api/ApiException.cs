using System.Net;
using System.Text.Json;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Api;

/// <summary>API'nin başarısız yanıtı; tüm gövde biçimleri tek bir <see cref="ApiError"/> listesine indirgenir.</summary>
public sealed class ApiException(HttpStatusCode? status, IReadOnlyList<ApiError> errors) : Exception(errors[0].Message)
{
    public HttpStatusCode? Status { get; } = status;
    public IReadOnlyList<ApiError> Errors { get; } = errors;

    public bool Has(string code) => Errors.Any(e => e.Code == code);

    /// <summary>
    /// Doğrulama hatalarını alan adına göre gruplar; anahtarlar camelCase ("DisplayName" → "displayName").
    /// </summary>
    public IReadOnlyDictionary<string, string> FieldErrors()
    {
        var result = new Dictionary<string, string>();
        foreach (var e in Errors)
        {
            if (e.Type != ErrorType.Validation || e.Code.Length == 0 || !char.IsLetter(e.Code[0])
                || e.Code.ToUpperInvariant() == e.Code || e.Code.Any(c => !char.IsLetterOrDigit(c) && c != '.'))
                continue;
            result.TryAdd(char.ToLowerInvariant(e.Code[0]) + e.Code[1..], e.Message);
        }
        return result;
    }

    public static ApiException Network() => Single(null, "NETWORK", Localizer.Instance.S.Errors.Network);

    public static ApiException Single(HttpStatusCode? status, string code, string message, ErrorType type = ErrorType.Failure)
        => new(status, [new ApiError(code, message, type)]);
}

/// <summary>
/// Backend'in üç hata gövdesini ayrıştırır (web: core/http/api-error.ts):
/// framework Result hataları (<c>ApiError[]</c>), problem+json (<c>{detail}</c>) ve ValidationProblemDetails
/// (<c>{errors: {alan: [mesaj]}}</c>).
/// </summary>
public static class ApiErrorParser
{
    public static ApiException Parse(HttpStatusCode status, string? body)
    {
        var s = Localizer.Instance.S.Errors;
        var code = (int)status;
        if (status == HttpStatusCode.TooManyRequests)
            return ApiException.Single(status, "RATE_LIMITED", s.RateLimited);

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0 && root.EnumerateArray().All(IsApiError))
                    return new ApiException(status, root.EnumerateArray().Select(ToError).ToList());

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var list = errors.EnumerateObject()
                            .SelectMany(field => field.Value.ValueKind == JsonValueKind.Array
                                ? field.Value.EnumerateArray().Select(m => new ApiError(field.Name, m.GetString() ?? "", ErrorType.Validation))
                                : [])
                            .ToList();
                        if (list.Count > 0)
                            return new ApiException(status, list);
                    }

                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String && code < 500)
                        return ApiException.Single(status, $"HTTP_{code}", detail.GetString()!, TypeFor(status));
                }
            }
            catch (JsonException)
            {
                // Gövde JSON değil: aşağıdaki genel mesaja düşer.
            }
        }

        return code >= 500
            ? ApiException.Single(status, "SERVER", s.Server)
            : ApiException.Single(status, $"HTTP_{code}", s.Request, TypeFor(status));
    }

    private static bool IsApiError(JsonElement e)
        => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("code", out _) && e.TryGetProperty("message", out _);

    private static ApiError ToError(JsonElement e)
    {
        var type = e.TryGetProperty("type", out var t) && Enum.TryParse<ErrorType>(t.GetString(), true, out var parsed)
            ? parsed
            : ErrorType.Failure;
        return new ApiError(e.GetProperty("code").GetString() ?? "", e.GetProperty("message").GetString() ?? "", type);
    }

    private static ErrorType TypeFor(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        _ => ErrorType.Failure
    };
}
