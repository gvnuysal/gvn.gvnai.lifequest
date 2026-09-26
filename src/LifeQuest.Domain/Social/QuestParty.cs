using System.Security.Cryptography;
using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Entities;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Social;

public enum PartyStatus
{
    Open = 1,
    /// <summary>Görevde kalan en az iki üyenin hepsi tamamladı; "birlikte" bonusu verildi.</summary>
    Completed = 2
}

/// <summary>
/// Quest Party: bir kullanıcı aktif görevi için davet bağlantısı oluşturur, en fazla 4 kişi katılır. Her üye görevi kendi
/// listesine alır ve kendi tamamlar. Görevde kalan herkes tamamlayınca (en az iki kişi) tamamlayanlar "birlikte" XP'si
/// alır. Görevi atlayan ya da süresi dolan üye beklenmez. Arkadaş listesi yoktur; üyeler yalnızca birbirinin görünen
/// adını ve tamamlayıp tamamlamadığını görür.
/// </summary>
public sealed class QuestParty : AggregateRoot
{
    public const int MaxMembers = 5;
    public const int InviteCodeLength = 10;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly List<PartyMember> _members = [];

    public Guid TemplateId { get; private set; }
    public string QuestTitle { get; private set; } = default!;
    public LifeCategory Category { get; private set; }
    public string InviteCode { get; private set; } = default!;
    public Guid HostUserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public PartyStatus Status { get; private set; } = PartyStatus.Open;
    public DateTime? SettledAt { get; private set; }

    public IReadOnlyCollection<PartyMember> Members => _members.AsReadOnly();

    private QuestParty() { }

    /// <summary>Kurucunun kabul edilmiş görevi için; parti o görevin süresi bitince kapanır.</summary>
    public static Result<QuestParty> Create(UserQuest hostQuest, string hostName, DateTime nowUtc)
    {
        if (hostQuest.Status != QuestStatus.Accepted)
            return Result<QuestParty>.Fail(PartyErrors.QuestNotActive);

        var party = new QuestParty
        {
            TemplateId = hostQuest.TemplateId,
            QuestTitle = hostQuest.Title,
            Category = hostQuest.Category,
            InviteCode = NewInviteCode(),
            HostUserId = hostQuest.UserId,
            ExpiresAt = hostQuest.ExpiresAt
        };
        party._members.Add(new PartyMember(party.Id, hostQuest.UserId, hostName, hostQuest.Id, nowUtc));
        return Result<QuestParty>.Ok(party);
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool IsJoinable(DateTime nowUtc) => Status == PartyStatus.Open && nowUtc < ExpiresAt && _members.Count < MaxMembers;

    public Result CanJoin(Guid userId, DateTime nowUtc)
    {
        if (IsMember(userId)) return Result.Fail(PartyErrors.AlreadyMember);
        if (Status != PartyStatus.Open || nowUtc >= ExpiresAt) return Result.Fail(PartyErrors.Closed);
        if (_members.Count >= MaxMembers) return Result.Fail(PartyErrors.Full);
        return Result.Ok();
    }

    public Result Join(Guid userId, string displayName, UserQuest quest, DateTime nowUtc)
    {
        var allowed = CanJoin(userId, nowUtc);
        if (!allowed.Succeeded) return allowed;
        if (quest.UserId != userId || quest.TemplateId != TemplateId) return Result.Fail(PartyErrors.QuestNotActive);

        _members.Add(new PartyMember(Id, userId, displayName, quest.Id, nowUtc));
        return Result.Ok();
    }

    /// <summary>Tamamlanmamış üye ayrılabilir; kurucu da ayrılabilir, parti diğerleriyle sürer.</summary>
    public Result<PartySettlement?> Leave(Guid userId, DateTime nowUtc)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) return Result<PartySettlement?>.Fail(PartyErrors.NotMember);
        if (member.CompletedAt is not null) return Result<PartySettlement?>.Fail(PartyErrors.AlreadyCompleted);

        _members.Remove(member);
        return Result<PartySettlement?>.Ok(TrySettle(nowUtc));
    }

    public PartySettlement? MarkCompleted(Guid userQuestId, int questLifeXp, DateTime nowUtc)
    {
        var member = _members.FirstOrDefault(m => m.UserQuestId == userQuestId);
        if (member is null || member.CompletedAt is not null || member.DroppedAt is not null) return null;

        member.Complete(nowUtc, questLifeXp);
        return TrySettle(nowUtc);
    }

    /// <summary>Üyenin görevi atlandı ya da süresi doldu: artık beklenmez.</summary>
    public PartySettlement? MarkDropped(Guid userQuestId, DateTime nowUtc)
    {
        var member = _members.FirstOrDefault(m => m.UserQuestId == userQuestId);
        if (member is null || member.CompletedAt is not null || member.DroppedAt is not null) return null;

        member.Drop(nowUtc);
        return TrySettle(nowUtc);
    }

    public static int BonusFor(int questLifeXp) => Math.Max(20, (int)Math.Round(questLifeXp * 0.25));

    /// <summary>
    /// Görevde kalan (atlamamış) en az iki üyenin hepsi tamamladıysa parti tamamlanır ve bonus verilir. Tek kişi kaldıysa
    /// parti süresi bitene kadar açık kalır: sonradan katılan biri tamamlarsa ikisi de bonusu alır.
    /// </summary>
    private PartySettlement? TrySettle(DateTime nowUtc)
    {
        if (Status != PartyStatus.Open) return null;

        var active = _members.Where(m => m.DroppedAt is null).ToList();
        if (active.Count < 2 || active.Any(m => m.CompletedAt is null)) return null;

        Status = PartyStatus.Completed;
        SettledAt = nowUtc;
        foreach (var member in active)
            member.AwardBonus(BonusFor(member.QuestLifeXp));
        return new PartySettlement(this, active);
    }

    private static string NewInviteCode()
        => new(RandomNumberGenerator.GetItems<char>(CodeAlphabet, InviteCodeLength));

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}

public sealed class PartyMember : Entity
{
    public Guid PartyId { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public Guid UserQuestId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DroppedAt { get; private set; }
    public int QuestLifeXp { get; private set; }
    public int BonusXp { get; private set; }

    private PartyMember() { }

    internal PartyMember(Guid partyId, Guid userId, string displayName, Guid userQuestId, DateTime nowUtc)
    {
        PartyId = partyId;
        UserId = userId;
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName));
        UserQuestId = userQuestId;
        JoinedAt = nowUtc;
    }

    internal void Complete(DateTime nowUtc, int questLifeXp)
    {
        CompletedAt = nowUtc;
        QuestLifeXp = questLifeXp;
    }

    internal void Drop(DateTime nowUtc) => DroppedAt = nowUtc;

    internal void AwardBonus(int xp) => BonusXp = xp;
}

/// <summary>Parti tamamlandı: <see cref="Completers"/> bonus XP alır.</summary>
public sealed record PartySettlement(QuestParty Party, IReadOnlyList<PartyMember> Completers);

public static class PartyErrors
{
    public static readonly Error NotFound = Error.NotFound("PARTY_NOT_FOUND", "Davet bulunamadı. Bağlantıyı kontrol et.");
    public static readonly Error QuestNotActive =
        Error.Conflict("PARTY_QUEST_NOT_ACTIVE", "Parti yalnızca kabul ettiğin, süresi dolmamış bir görev için açılabilir.");
    public static readonly Error AlreadyMember = Error.Conflict("PARTY_ALREADY_MEMBER", "Bu partidesin.");
    public static readonly Error Closed = Error.Conflict("PARTY_CLOSED", "Bu parti artık katılıma açık değil.");
    public static readonly Error Full = Error.Conflict("PARTY_FULL", $"Parti dolu (en fazla {QuestParty.MaxMembers} kişi).");
    public static readonly Error NotMember = Error.NotFound("PARTY_NOT_MEMBER", "Bu partinin üyesi değilsin.");
    public static readonly Error AlreadyCompleted = Error.Conflict("PARTY_ALREADY_COMPLETED", "Görevi tamamladıktan sonra partiden ayrılamazsın.");
    public static readonly Error AlreadyHasParty = Error.Conflict("PARTY_EXISTS", "Bu görev zaten bir partide.");
}

public interface IQuestPartyRepository : IRepository<QuestParty>
{
    Task<QuestParty?> GetByCodeAsync(string inviteCode, CancellationToken cancellationToken = default);

    /// <summary>Üyenin görevi üzerinden (tamamlama/atlama/süre dolumu anında).</summary>
    Task<QuestParty?> GetByUserQuestAsync(Guid userQuestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuestParty>> GetByUserQuestsAsync(IReadOnlyCollection<Guid> userQuestIds, CancellationToken cancellationToken = default);
}
