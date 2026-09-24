using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;

namespace LifeQuest.Domain.Identity;

public sealed class UserAccount : AggregateRoot
{
    public const int MinimumAge = 18;
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;

    /// <summary>Veri minimizasyonu: doğum tarihi yerine yalnızca yıl tutulur.</summary>
    public int BirthYear { get; private set; }

    public string Role { get; private set; } = UserRoles.User;
    public int FailedLoginCount { get; private set; }
    public DateTime? LockoutEndsAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private UserAccount() { }

    public static Result<UserAccount> Register(
        string email, string passwordHash, string displayName, int birthYear, DateTime nowUtc)
    {
        // Yalnızca yıl bilindiği için muhafazakâr hesap: bu yıl doğum günü gelmemiş olabilir.
        if (nowUtc.Year - birthYear - 1 < MinimumAge)
            return Result<UserAccount>.Fail(IdentityErrors.Underage);

        var account = new UserAccount
        {
            Email = NormalizeEmail(Guard.NotNullOrWhiteSpace(email, nameof(email))),
            PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash)),
            DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim(),
            BirthYear = birthYear
        };

        account.AddDomainEvent(new UserRegisteredEvent(account.Id));
        return Result<UserAccount>.Ok(account);
    }

    /// <returns>Rol değiştiyse <c>true</c>.</returns>
    public bool GrantRole(string role)
    {
        Guard.True(role is UserRoles.User or UserRoles.Admin, "Bilinmeyen rol.");
        if (Role == role)
            return false;

        Role = role;
        return true;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndsAt > nowUtc;

    public void RegisterFailedLogin(DateTime nowUtc)
    {
        FailedLoginCount++;
        if (FailedLoginCount < MaxFailedLoginAttempts)
            return;

        LockoutEndsAt = nowUtc.Add(LockoutDuration);
        FailedLoginCount = 0;
    }

    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        FailedLoginCount = 0;
        LockoutEndsAt = null;
        LastLoginAt = nowUtc;
    }
}

public static class UserRoles
{
    public const string User = "user";
    public const string Admin = "admin";
}
