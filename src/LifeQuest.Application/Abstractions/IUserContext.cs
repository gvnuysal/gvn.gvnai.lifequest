namespace LifeQuest.Application.Abstractions;

/// <summary>
/// Kimliği doğrulanmış kullanıcı. UserId asla istemci gövdesinden alınmaz; yalnızca token'daki
/// principal'dan çözülür (yatay erişim koruması).
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }

    /// <summary>Kimlik doğrulanmamışsa <c>UnauthorizedException</c> fırlatır.</summary>
    Guid UserId { get; }
}
