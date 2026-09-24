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
}

public sealed record SkipQuestRequest(SkipReason Reason);

public sealed record QuestFeedbackRequest(int? Rating, FeedbackPreference? Preference);
