using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LifeQuest.Application.Notifications;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PushTests(LifeQuestApiFactory factory)
{
    private static object Subscription(string endpoint)
        => new { endpoint, keys = new { p256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM", auth = "tBHItJI5svbpez7KI4CCXg" } };

    private static string NewEndpoint(string kind = "ok") => $"https://push.example.com/{kind}/{Guid.NewGuid():N}";

    [Fact]
    public async Task Subscription_is_stored_per_device_and_test_push_reaches_it()
    {
        var client = await factory.CreateUserClientAsync();

        var settings = await client.GetFromJsonAsync<JsonElement>("/api/v1/push", Json);
        Assert.True(settings.GetProperty("enabled").GetBoolean());
        Assert.Equal(0, settings.GetProperty("devices").GetInt32());

        // Test bildirimi için önce cihaz gerekir.
        await AssertErrorAsync(await client.PostAsync("/api/v1/push/test", null), HttpStatusCode.Conflict, "PUSH_NO_DEVICES");

        var endpoint = NewEndpoint();
        var subscribed = await client.PutAsJsonAsync("/api/v1/push/subscription", Subscription(endpoint));
        Assert.Equal(1, (await subscribed.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("devices").GetInt32());
        // Aynı tarayıcı tekrar abone olursa ikinci kayıt açılmaz.
        await client.PutAsJsonAsync("/api/v1/push/subscription", Subscription(endpoint));

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/push/test", null)).StatusCode);
        Assert.Contains(factory.Push.Sent, s => s.Endpoint == endpoint && s.Notification.Url == "/today");

        var unsubscribe = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/push/subscription") { Content = JsonContent.Create(new { endpoint }) };
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(unsubscribe)).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/v1/push", Json)).GetProperty("devices").GetInt32());
    }

    [Theory]
    [InlineData("http://push.example.com/insecure")]
    [InlineData("https://localhost/internal")]
    [InlineData("file:///etc/passwd")]
    public async Task Only_public_https_push_endpoints_are_accepted(string endpoint)
    {
        var client = await factory.CreateUserClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/push/subscription", Subscription(endpoint))).StatusCode);
    }

    [Fact]
    public async Task Device_moves_to_the_account_that_signs_in_last_and_expired_ones_are_dropped()
    {
        var first = await factory.CreateUserClientAsync();
        var second = await factory.CreateUserClientAsync();
        var shared = NewEndpoint();

        await first.PutAsJsonAsync("/api/v1/push/subscription", Subscription(shared));
        await second.PutAsJsonAsync("/api/v1/push/subscription", Subscription(shared));
        Assert.Equal(0, (await first.GetFromJsonAsync<JsonElement>("/api/v1/push", Json)).GetProperty("devices").GetInt32());

        // Push servisi aboneliği tanımıyorsa (410) kayıt silinir.
        await second.PutAsJsonAsync("/api/v1/push/subscription", Subscription(NewEndpoint("gone")));
        Assert.Equal(2, (await second.GetFromJsonAsync<JsonElement>("/api/v1/push", Json)).GetProperty("devices").GetInt32());
        await second.PostAsync("/api/v1/push/test", null);
        Assert.Equal(1, (await second.GetFromJsonAsync<JsonElement>("/api/v1/push", Json)).GetProperty("devices").GetInt32());
    }

    [Fact]
    public async Task Daily_reminder_is_sent_once_at_the_chosen_local_hour_and_skipped_after_a_completion()
    {
        // Şu an yerel saati 08-21 arasında olan bir saat dilimi seçilir; test günün her saatinde çalışır.
        // Saat sınırında başlamamak için önce beklenir; yerel saat bekledikten sonra hesaplanır.
        if (DateTime.UtcNow is { Minute: >= 58 } now)
            await Task.Delay(TimeSpan.FromMinutes(60 - now.Minute) - TimeSpan.FromSeconds(now.Second) + TimeSpan.FromSeconds(2));
        var (timeZoneId, localHour) = Enumerable.Range(-12, 27)
            .Select(offset => ($"Etc/GMT{(offset <= 0 ? "+" : "-")}{Math.Abs(offset)}", offset))
            .Select(z => (z.Item1, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(z.Item1)).Hour))
            .First(z => z.Hour is >= 8 and <= 21);

        var reminded = await factory.CreateUserClientAsync();
        var busy = await factory.CreateUserClientAsync();
        var remindedEndpoint = NewEndpoint();
        var busyEndpoint = NewEndpoint();

        foreach (var (client, endpoint) in new[] { (reminded, remindedEndpoint), (busy, busyEndpoint) })
        {
            await CompleteOnboardingAsync(client);
            var update = await client.PatchAsJsonAsync("/api/v1/profile/preferences", new { timeZoneId, dailyReminderHour = localHour }, Json);
            Assert.Equal(localHour, (await update.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("dailyReminderHour").GetInt32());
            await client.PutAsJsonAsync("/api/v1/push/subscription", Subscription(endpoint));
        }

        // İkinci kullanıcı bugün bir görev tamamlar: hatırlatma gerekmez.
        var today = await busy.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var questId = today.GetProperty("quests")[0].GetProperty("id").GetGuid();
        await busy.PostAsync($"/api/v1/quests/{questId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, (await busy.PostAsync($"/api/v1/quests/{questId}/complete", null)).StatusCode);

        await RunRemindersAsync();
        await RunRemindersAsync();

        var reminders = factory.Push.Sent.Where(s => s.Notification.Tag == "daily-reminder").ToList();
        var mine = Assert.Single(reminders, s => s.Endpoint == remindedEndpoint);
        Assert.Contains("öneri", mine.Notification.Body);
        Assert.DoesNotContain(reminders, s => s.Endpoint == busyEndpoint);

        // Gece saatleri seçilemez.
        await AssertErrorAsync(await reminded.PatchAsJsonAsync("/api/v1/profile/preferences", new { dailyReminderHour = 3 }, Json),
            HttpStatusCode.BadRequest, "DailyReminderHour");
    }

    private async Task RunRemindersAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DailyReminderService>().SendDueAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Deleting_the_account_removes_its_push_devices()
    {
        var email = $"push-del-{Guid.NewGuid():N}@example.com";
        var client = await factory.CreateUserClientAsync(email);
        var endpoint = NewEndpoint();
        await client.PutAsJsonAsync("/api/v1/push/subscription", Subscription(endpoint));

        var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/profile") { Content = JsonContent.Create(new { password = Password }) };
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(delete)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<LifeQuestDbContext>().PushSubscriptions.AnyAsync(p => p.Endpoint == endpoint));
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        Assert.Contains(code, body);
    }
}
