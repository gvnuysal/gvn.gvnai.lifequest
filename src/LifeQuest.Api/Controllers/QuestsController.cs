using Gvn.GvnFramework.AspNetCore.Controllers;
using LifeQuest.Api.Infrastructure;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Quests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LifeQuest.Api.Controllers;

[Authorize]
[Route("api/v1/quests")]
public sealed class QuestsController(ISender sender) : ApiControllerBase
{
    /// <summary>Bugünün 3 önerisi. İlk çağrıda üretilir, sonraki çağrılar aynı sonucu döner.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetTodayQuestsCommand(), cancellationToken));

    /// <summary>Bağlamsal öneri: "Bu akşam 2 saatim var".</summary>
    [HttpPost("suggestions")]
    [EnableRateLimiting(RateLimitingExtensions.SuggestionPolicy)]
    public async Task<IActionResult> Suggest(SuggestQuestsCommand command, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(command, cancellationToken));

    [HttpGet("active")]
    public async Task<IActionResult> Active(CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetActiveQuestsQuery(), cancellationToken));

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, [FromQuery] QuestStatus? status = null,
        CancellationToken cancellationToken = default)
        => HandleResult(await sender.Send(new GetQuestHistoryQuery(pageNumber, pageSize, status), cancellationToken));

    /// <summary>Quest detayı ve "neden bunu önerdim?" skor dökümü.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new GetQuestQuery(id), cancellationToken));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new AcceptQuestCommand(id), cancellationToken));

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new CompleteQuestCommand(id), cancellationToken));

    [HttpPost("{id:guid}/skip")]
    public async Task<IActionResult> Skip(Guid id, SkipQuestRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SkipQuestCommand(id, request.Reason), cancellationToken));

    [HttpPost("{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, QuestFeedbackRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SubmitQuestFeedbackCommand(id, request.Rating, request.Preference), cancellationToken));

    /// <summary>Quest'in deneyimini "sonra yaparım" listesine ekler.</summary>
    [HttpPost("{id:guid}/save")]
    public async Task<IActionResult> Save(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new SaveQuestCommand(id), cancellationToken));

    /// <summary>Kabul edilmiş quest'i kullanıcının yerel saatiyle planlar; <c>null</c> planı kaldırır.</summary>
    [HttpPut("{id:guid}/plan")]
    public async Task<IActionResult> Plan(Guid id, PlanRequest request, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new PlanQuestCommand(id, request.PlannedAtLocal), cancellationToken));

    /// <summary>Kabul edilmiş görev için Quest Party davet bağlantısı; zaten varsa aynı partiyi döner.</summary>
    [HttpPost("{id:guid}/party")]
    public async Task<IActionResult> Party(Guid id, CancellationToken cancellationToken)
        => HandleResult(await sender.Send(new LifeQuest.Application.Social.CreatePartyCommand(id), cancellationToken));

    /// <summary>Planlanmış quest için iCalendar (.ics) dosyası; konum bilgisi içermez.</summary>
    [HttpGet("{id:guid}/calendar.ics")]
    public async Task<IActionResult> Calendar(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuestCalendarQuery(id), cancellationToken);
        return result.Succeeded
            ? File(System.Text.Encoding.UTF8.GetBytes(result.Data!.Content), "text/calendar; charset=utf-8", result.Data.FileName)
            : HandleResult(result);
    }

    public sealed record PlanRequest(DateTime? PlannedAtLocal);
}

public sealed record SkipQuestRequest(SkipReason Reason);

public sealed record QuestFeedbackRequest(int? Rating, FeedbackPreference? Preference);
