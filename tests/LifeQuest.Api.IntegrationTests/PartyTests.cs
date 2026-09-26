using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static LifeQuest.Api.IntegrationTests.LifeQuestApiFactory;

namespace LifeQuest.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PartyTests(LifeQuestApiFactory factory)
{
    private async Task<HttpClient> OnboardedAsync()
    {
        var client = await factory.CreateUserClientAsync();
        await CompleteOnboardingAsync(client);
        return client;
    }

    private static async Task<Guid> AcceptFirstTodayAsync(HttpClient client)
    {
        var today = await client.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json);
        var id = today.GetProperty("quests")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/quests/{id}/accept", null)).StatusCode);
        return id;
    }

    private static async Task<int> LifeXpAsync(HttpClient client)
        => (await client.GetFromJsonAsync<JsonElement>("/api/v1/progress", Json)).GetProperty("lifeXp").GetInt32();

    private static async Task<string> CreatePartyAsync(HttpClient host, Guid questId)
    {
        var response = await host.PostAsync($"/api/v1/quests/{questId}/party", null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("inviteCode").GetString()!;
    }

    private static async Task<Guid> JoinAsync(HttpClient client, string code)
    {
        var response = await client.PostAsync($"/api/v1/parties/{code}/join", null);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("myQuestId").GetGuid();
    }

    [Fact]
    public async Task Invitee_joins_by_link_and_both_get_the_together_bonus_when_everyone_completes()
    {
        var host = await OnboardedAsync();
        var guest = await OnboardedAsync();
        var hostQuest = await AcceptFirstTodayAsync(host);

        var code = await CreatePartyAsync(host, hostQuest);
        Assert.Equal(code, await CreatePartyAsync(host, hostQuest)); // Tekrar çağrı aynı partiyi döner.

        var invite = await guest.GetFromJsonAsync<JsonElement>($"/api/v1/parties/{code.ToLowerInvariant()}", Json);
        Assert.Equal("Test Kullanıcı", invite.GetProperty("hostName").GetString());
        Assert.Equal(1, invite.GetProperty("memberCount").GetInt32());
        Assert.True(invite.GetProperty("isJoinable").GetBoolean());
        Assert.False(invite.TryGetProperty("members", out _)); // Davetli üyeleri göremez.

        var guestQuest = await JoinAsync(guest, code);
        await AssertErrorAsync(await guest.PostAsync($"/api/v1/parties/{code}/join", null), HttpStatusCode.Conflict, "PARTY_ALREADY_MEMBER");

        var detail = await guest.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{guestQuest}", Json);
        var party = detail.GetProperty("party");
        Assert.Equal(2, party.GetProperty("members").GetArrayLength());
        Assert.Contains(party.GetProperty("members").EnumerateArray(), m => m.GetProperty("isYou").GetBoolean() && !m.GetProperty("isHost").GetBoolean());
        Assert.Equal("Accepted", detail.GetProperty("quest").GetProperty("status").GetString());

        // İlk tamamlayan bonus almaz (diğeri hâlâ görevde); ikinci tamamlamada ikisi de alır.
        var hostDone = await (await host.PostAsync($"/api/v1/quests/{hostQuest}/complete", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(0, hostDone.GetProperty("partyBonusXp").GetInt32());
        var hostXpBefore = await LifeXpAsync(host);

        var guestDone = await (await guest.PostAsync($"/api/v1/quests/{guestQuest}/complete", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        var bonus = guestDone.GetProperty("partyBonusXp").GetInt32();
        Assert.True(bonus >= 20);
        Assert.True(await LifeXpAsync(host) > hostXpBefore);

        var settled = (await host.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{hostQuest}", Json)).GetProperty("party");
        Assert.Equal("Completed", settled.GetProperty("status").GetString());
        Assert.All(settled.GetProperty("members").EnumerateArray(), m => Assert.True(m.GetProperty("bonusXp").GetInt32() > 0));

        await AssertErrorAsync(await (await OnboardedAsync()).PostAsync($"/api/v1/parties/{code}/join", null), HttpStatusCode.Conflict, "PARTY_CLOSED");
    }

    [Fact]
    public async Task A_member_who_skips_does_not_block_the_others()
    {
        var host = await OnboardedAsync();
        var quitter = await OnboardedAsync();
        var friend = await OnboardedAsync();
        var hostQuest = await AcceptFirstTodayAsync(host);
        var code = await CreatePartyAsync(host, hostQuest);
        var quitterQuest = await JoinAsync(quitter, code);
        var friendQuest = await JoinAsync(friend, code);

        await host.PostAsync($"/api/v1/quests/{hostQuest}/complete", null);
        await quitter.PostAsJsonAsync($"/api/v1/quests/{quitterQuest}/skip", new { reason = "NoTime" }, Json);
        var done = await (await friend.PostAsync($"/api/v1/quests/{friendQuest}/complete", null)).Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(done.GetProperty("partyBonusXp").GetInt32() > 0);

        var members = (await friend.GetFromJsonAsync<JsonElement>($"/api/v1/quests/{friendQuest}", Json)).GetProperty("party").GetProperty("members");
        var dropped = Assert.Single(members.EnumerateArray(), m => m.GetProperty("dropped").GetBoolean());
        Assert.Equal(0, dropped.GetProperty("bonusXp").GetInt32());
    }

    [Fact]
    public async Task Parties_need_an_accepted_quest_known_code_and_free_seats()
    {
        var host = await OnboardedAsync();
        var offered = (await host.GetFromJsonAsync<JsonElement>("/api/v1/quests/today", Json)).GetProperty("quests")[1].GetProperty("id").GetGuid();
        await AssertErrorAsync(await host.PostAsync($"/api/v1/quests/{offered}/party", null), HttpStatusCode.Conflict, "PARTY_QUEST_NOT_ACTIVE");
        await AssertErrorAsync(await host.GetAsync("/api/v1/parties/NOSUCHCODE"), HttpStatusCode.NotFound, "PARTY_NOT_FOUND");

        // Başkasının görevi için parti açılamaz.
        var stranger = await OnboardedAsync();
        await AssertErrorAsync(await stranger.PostAsync($"/api/v1/quests/{offered}/party", null), HttpStatusCode.NotFound, "QUEST_NOT_FOUND");

        var code = await CreatePartyAsync(host, await AcceptFirstTodayAsync(host));
        for (var i = 0; i < 4; i++)
            await JoinAsync(await OnboardedAsync(), code);
        await AssertErrorAsync(await (await OnboardedAsync()).PostAsync($"/api/v1/parties/{code}/join", null), HttpStatusCode.Conflict, "PARTY_FULL");
    }

    [Fact]
    public async Task Member_can_leave_before_completing()
    {
        var host = await OnboardedAsync();
        var guest = await OnboardedAsync();
        var code = await CreatePartyAsync(host, await AcceptFirstTodayAsync(host));
        await JoinAsync(guest, code);

        Assert.Equal(HttpStatusCode.OK, (await guest.DeleteAsync($"/api/v1/parties/{code}/members/me")).StatusCode);
        var invite = await guest.GetFromJsonAsync<JsonElement>($"/api/v1/parties/{code}", Json);
        Assert.False(invite.GetProperty("isMember").GetBoolean());
        Assert.Equal(1, invite.GetProperty("memberCount").GetInt32());
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{response.StatusCode}: {body}");
        Assert.Contains(code, body);
    }
}
