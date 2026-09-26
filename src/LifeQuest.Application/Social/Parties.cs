using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Social;

namespace LifeQuest.Application.Social;

public sealed record PartyMemberDto(string DisplayName, bool IsHost, bool IsYou, bool Completed, bool Dropped, int BonusXp);

/// <summary>Üyelerin gördüğü parti: görünen adlar ve kimin tamamladığı. E-posta ya da kullanıcı kimliği yoktur.</summary>
public sealed record PartyDto(
    string InviteCode,
    string QuestTitle,
    LifeCategory Category,
    PartyStatus Status,
    DateTime ExpiresAt,
    int MaxMembers,
    bool IsJoinable,
    IReadOnlyList<PartyMemberDto> Members);

/// <summary>Davet bağlantısını açan (henüz üye olmayan) kişinin gördüğü özet: yalnızca kurucunun adı ve üye sayısı.</summary>
public sealed record PartyInviteDto(
    string InviteCode,
    string QuestTitle,
    LifeCategory Category,
    string HostName,
    int MemberCount,
    int MaxMembers,
    DateTime ExpiresAt,
    bool IsMember,
    bool IsJoinable,
    Guid? MyQuestId);

internal static class PartyMapping
{
    public static PartyDto ToDto(this QuestParty party, Guid viewerId, DateTime nowUtc) => new(
        party.InviteCode, party.LocalizedQuestTitle.Current, party.Category, party.Status, party.ExpiresAt, QuestParty.MaxMembers,
        party.IsJoinable(nowUtc),
        party.Members.OrderBy(m => m.JoinedAt).Select(m => new PartyMemberDto(
            m.DisplayName, m.UserId == party.HostUserId, m.UserId == viewerId,
            m.CompletedAt is not null, m.DroppedAt is not null, m.BonusXp)).ToList());
}

// ── Oluşturma ─────────────────────────────────────────────────────────────────

/// <summary>Kabul edilmiş görev için parti açar; görevin zaten partisi varsa onu döner (tekrar çağrılabilir).</summary>
public sealed record CreatePartyCommand(Guid QuestId) : ICommand<PartyDto>;

internal sealed class CreatePartyCommandHandler(
    IUserQuestRepository quests,
    IQuestPartyRepository parties,
    IUserAccountRepository accounts,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreatePartyCommand, PartyDto>
{
    public async Task<Result<PartyDto>> Handle(CreatePartyCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var quest = await quests.GetForUserAsync(command.QuestId, user.UserId, cancellationToken);
        if (quest is null)
            return Result<PartyDto>.Fail(QuestErrors.NotFound);

        if (await parties.GetByUserQuestAsync(quest.Id, cancellationToken) is { } existing)
            return Result<PartyDto>.Ok(existing.ToDto(user.UserId, now));

        if (quest.Status != QuestStatus.Accepted || quest.ExpiresAt <= now)
            return Result<PartyDto>.Fail(PartyErrors.QuestNotActive);

        var account = await accounts.GetByIdAsync(user.UserId, cancellationToken);
        var created = QuestParty.Create(quest, account?.DisplayName ?? "LifeQuest", now);
        if (!created.Succeeded)
            return Result<PartyDto>.Fail(created.Errors);

        await parties.AddAsync(created.Data!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PartyDto>.Ok(created.Data!.ToDto(user.UserId, now));
    }
}

// ── Davet önizleme ────────────────────────────────────────────────────────────

public sealed record GetPartyInviteQuery(string Code) : IQuery<PartyInviteDto>;

public sealed class GetPartyInviteQueryValidator : AbstractValidator<GetPartyInviteQuery>
{
    public GetPartyInviteQueryValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(QuestParty.InviteCodeLength + 4);
}

internal sealed class GetPartyInviteQueryHandler(IQuestPartyRepository parties, IUserContext user, TimeProvider clock)
    : IQueryHandler<GetPartyInviteQuery, PartyInviteDto>
{
    public async Task<Result<PartyInviteDto>> Handle(GetPartyInviteQuery query, CancellationToken cancellationToken)
    {
        var party = await parties.GetByCodeAsync(QuestParty.NormalizeCode(query.Code), cancellationToken);
        if (party is null)
            return Result<PartyInviteDto>.Fail(PartyErrors.NotFound);

        var now = clock.GetUtcNow().UtcDateTime;
        var host = party.Members.FirstOrDefault(m => m.UserId == party.HostUserId)?.DisplayName
                   ?? party.Members.OrderBy(m => m.JoinedAt).First().DisplayName;
        var me = party.Members.FirstOrDefault(m => m.UserId == user.UserId);
        return Result<PartyInviteDto>.Ok(new PartyInviteDto(
            party.InviteCode, party.LocalizedQuestTitle.Current, party.Category, host, party.Members.Count, QuestParty.MaxMembers,
            party.ExpiresAt, me is not null, party.IsJoinable(now) && me is null, me?.UserQuestId));
    }
}

// ── Katılma / ayrılma ─────────────────────────────────────────────────────────

public sealed record JoinPartyCommand(string Code) : ICommand<PartyInviteDto>;

internal sealed class JoinPartyCommandHandler(
    IQuestPartyRepository parties,
    IUserQuestRepository quests,
    IUserAccountRepository accounts,
    QuestOfferService offers,
    Microsoft.Extensions.Options.IOptions<QuestOptions> questOptions,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<JoinPartyCommand, PartyInviteDto>
{
    public async Task<Result<PartyInviteDto>> Handle(JoinPartyCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var party = await parties.GetByCodeAsync(QuestParty.NormalizeCode(command.Code), cancellationToken);
        if (party is null)
            return Result<PartyInviteDto>.Fail(PartyErrors.NotFound);

        var allowed = party.CanJoin(user.UserId, now);
        if (!allowed.Succeeded)
            return Result<PartyInviteDto>.Fail(allowed.Errors);

        // Aynı görev zaten açıksa (ör. bugünkü önerilerde) onu kullanır; yoksa motorun güvenlik filtrelerinden
        // (efor sınırı, bütçe, gece açık hava…) geçirerek yeni bir görev başlatır.
        var quest = await quests.GetOpenForTemplateAsync(user.UserId, party.TemplateId, now, cancellationToken);
        if (quest is { Status: QuestStatus.Offered })
        {
            if (await quests.CountAcceptedAsync(user.UserId, cancellationToken) >= questOptions.Value.MaxActiveQuests)
                return Result<PartyInviteDto>.Fail(QuestErrors.TooManyActiveQuests(questOptions.Value.MaxActiveQuests));
            var accepted = quest.Accept(now);
            if (!accepted.Succeeded)
                return Result<PartyInviteDto>.Fail(accepted.Errors);
        }

        if (quest is null)
        {
            var started = await offers.StartTemplateAsync(user.UserId, party.TemplateId, cancellationToken, QuestSource.Party);
            if (!started.Succeeded)
                return Result<PartyInviteDto>.Fail(started.Errors);
            quest = started.Data!;
        }
        else if (await parties.GetByUserQuestAsync(quest.Id, cancellationToken) is not null)
        {
            return Result<PartyInviteDto>.Fail(PartyErrors.AlreadyHasParty);
        }

        var account = await accounts.GetByIdAsync(user.UserId, cancellationToken);
        var joined = party.Join(user.UserId, account?.DisplayName ?? "LifeQuest", quest, now);
        if (!joined.Succeeded)
            return Result<PartyInviteDto>.Fail(joined.Errors);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var host = party.Members.First(m => m.UserId == party.HostUserId || m.JoinedAt == party.Members.Min(x => x.JoinedAt)).DisplayName;
        return Result<PartyInviteDto>.Ok(new PartyInviteDto(
            party.InviteCode, party.LocalizedQuestTitle.Current, party.Category, host, party.Members.Count, QuestParty.MaxMembers,
            party.ExpiresAt, true, false, quest.Id));
    }
}

public sealed record LeavePartyCommand(string Code) : ICommand;

internal sealed class LeavePartyCommandHandler(
    IQuestPartyRepository parties,
    PartyService partyService,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<LeavePartyCommand>
{
    public async Task<Result> Handle(LeavePartyCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var party = await parties.GetByCodeAsync(QuestParty.NormalizeCode(command.Code), cancellationToken);
        if (party is null)
            return Result.Fail(PartyErrors.NotFound);

        var left = party.Leave(user.UserId, now);
        if (!left.Succeeded)
            return Result.Fail(left.Errors);

        if (left.Data is { } settlement)
            await partyService.AwardAsync(settlement, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await partyService.NotifyAsync(left.Data, cancellationToken);
        return Result.Ok();
    }
}
