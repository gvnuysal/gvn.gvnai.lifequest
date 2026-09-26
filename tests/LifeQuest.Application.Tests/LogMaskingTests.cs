using Gvn.GvnFramework.Core.Observability;
using LifeQuest.Application.Admin.Users;
using LifeQuest.Application.Identity;

namespace LifeQuest.Application.Tests;

/// <summary>
/// Log maskeleme framework'te (Gvn.GvnFramework 1.1.0): Serilog politikası ve LoggingBehavior aynı
/// <see cref="PayloadSnapshotFactory"/>'yi kullanır. Burada LifeQuest tiplerinin doğru işaretlendiği doğrulanır.
/// </summary>
public sealed class LogMaskingTests
{
    [Fact]
    public void Register_and_login_never_expose_the_password_and_only_a_hint_of_the_email()
    {
        foreach (var request in new object[]
                 {
                     new RegisterCommand("ayse.yilmaz@example.com", "Gizli-Sifre-123", "Ayşe", 1990),
                     new LoginCommand("ayse.yilmaz@example.com", "Gizli-Sifre-123")
                 })
        {
            Assert.True(PayloadSnapshotFactory.HasSensitiveMembers(request.GetType()));
            var snapshot = PayloadSnapshotFactory.Create(request);

            Assert.DoesNotContain("Gizli-Sifre-123", snapshot.Values.Select(v => v?.ToString()));
            var email = snapshot["Email"]?.ToString();
            Assert.NotEqual("ayse.yilmaz@example.com", email);
            Assert.EndsWith("@example.com", email);
        }
    }

    [Fact]
    public void Tokens_and_confirmation_emails_are_masked()
    {
        var refresh = PayloadSnapshotFactory.Create(new RefreshTokenCommand("refresh-token-value"));
        Assert.DoesNotContain("refresh-token-value", refresh.Values.Select(v => v?.ToString()));

        var delete = PayloadSnapshotFactory.Create(new DeleteUserCommand(Guid.NewGuid(), "Talep", "ayse.yilmaz@example.com"));
        Assert.NotEqual("ayse.yilmaz@example.com", delete["ConfirmEmail"]?.ToString());
    }
}
