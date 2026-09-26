using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Repositories;

namespace LifeQuest.Domain.RealWorld;

public enum LocalPlaceKind
{
    Venue = 1,
    Event = 2
}

/// <summary>
/// Yöneticinin şehir bazında girdiği mekân ya da etkinlik; görev template'lerine bağlanır. Kullanıcıya görev detayında
/// "şehrinde" önerisi olarak gösterilir; yaklaşan etkinliği olan görevler öneride öne çıkar. Kullanıcıdan konum istenmez:
/// eşleşme yalnızca profildeki şehir adıyla yapılır.
/// </summary>
public sealed class LocalPlace : AggregateRoot
{
    public const int MaxTemplates = 20;
    public static readonly TimeSpan MaxEventDuration = TimeSpan.FromDays(60);

    public LocalPlaceKind Kind { get; private set; }
    public string City { get; private set; } = default!;
    public string CityKey { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Url { get; private set; }
    public string? Note { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public List<Guid> TemplateIds { get; private set; } = [];
    public bool IsActive { get; private set; } = true;
    public string CreatedByEmail { get; private set; } = default!;

    private LocalPlace() { }

    public static Result<LocalPlace> Create(LocalPlaceDraft draft, string createdByEmail)
    {
        var place = new LocalPlace { CreatedByEmail = Guard.NotNullOrWhiteSpace(createdByEmail, nameof(createdByEmail)) };
        var applied = place.Apply(draft);
        return applied.Succeeded ? Result<LocalPlace>.Ok(place) : Result<LocalPlace>.Fail(applied.Errors);
    }

    public Result Update(LocalPlaceDraft draft) => Apply(draft);

    public void SetActive(bool active) => IsActive = active;

    /// <summary>Etkinlik bitmemişse ve verilen pencereyle kesişiyorsa; mekânlar her zaman geçerlidir.</summary>
    public bool IsRelevantBetween(DateTime fromUtc, DateTime toUtc)
        => IsActive && (Kind == LocalPlaceKind.Venue || (StartsAt <= toUtc && EndsAt >= fromUtc));

    private Result Apply(LocalPlaceDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.City) || string.IsNullOrWhiteSpace(draft.Name))
            return Result.Fail(LocalPlaceErrors.NameAndCityRequired);
        if (draft.TemplateIds.Count == 0 || draft.TemplateIds.Count > MaxTemplates)
            return Result.Fail(LocalPlaceErrors.TemplatesRequired);
        if (draft.Url is { } url && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            return Result.Fail(LocalPlaceErrors.InvalidUrl);

        if (draft.Kind == LocalPlaceKind.Event)
        {
            if (draft.StartsAt is not { } start || draft.EndsAt is not { } end || end < start)
                return Result.Fail(LocalPlaceErrors.EventNeedsDates);
            if (end - start > MaxEventDuration)
                return Result.Fail(LocalPlaceErrors.EventTooLong);
        }

        Kind = draft.Kind;
        City = draft.City.Trim();
        CityKey = RealWorld.CityKey.Normalize(draft.City);
        Name = draft.Name.Trim();
        Address = Clean(draft.Address);
        Url = Clean(draft.Url);
        Note = Clean(draft.Note);
        StartsAt = draft.Kind == LocalPlaceKind.Event ? draft.StartsAt : null;
        EndsAt = draft.Kind == LocalPlaceKind.Event ? draft.EndsAt : null;
        TemplateIds = draft.TemplateIds.Distinct().ToList();
        return Result.Ok();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <param name="StartsAt">Etkinlik başlangıcı (UTC).</param>
public sealed record LocalPlaceDraft(
    LocalPlaceKind Kind,
    string City,
    string Name,
    string? Address,
    string? Url,
    string? Note,
    DateTime? StartsAt,
    DateTime? EndsAt,
    IReadOnlyList<Guid> TemplateIds);

public static class LocalPlaceErrors
{
    public static readonly Error NotFound = Error.NotFound("PLACE_NOT_FOUND", "Mekân veya etkinlik bulunamadı.");

    public static readonly Error NameAndCityRequired = Error.Validation("Name", "Ad ve şehir zorunlu.");

    public static readonly Error TemplatesRequired =
        Error.Validation("TemplateIds", $"En az 1, en fazla {LocalPlace.MaxTemplates} göreve bağlanmalı.");

    public static readonly Error InvalidUrl = Error.Validation("Url", "Bağlantı https:// ile başlamalı.");

    public static readonly Error EventNeedsDates =
        Error.Validation("StartsAt", "Etkinliğin başlangıç ve bitiş zamanı olmalı; bitiş başlangıçtan önce olamaz.");

    public static readonly Error EventTooLong = Error.Validation("EndsAt", "Etkinlik en fazla 60 gün sürebilir.");
}

public interface ILocalPlaceRepository : IRepository<LocalPlace>
{
    Task<IReadOnlyList<LocalPlace>> GetAllAsync(string? cityKey, CancellationToken cancellationToken = default);

    /// <summary>Şehirde, pencereyle kesişen aktif etkinliklere bağlı template'ler.</summary>
    Task<IReadOnlySet<Guid>> GetEventTemplateIdsAsync(string cityKey, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Şehirde template'e bağlı aktif mekânlar ve bitmemiş etkinlikler.</summary>
    Task<IReadOnlyList<LocalPlace>> GetForTemplateAsync(string cityKey, Guid templateId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
