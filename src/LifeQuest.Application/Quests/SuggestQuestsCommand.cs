using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;

namespace LifeQuest.Application.Quests;

/// <summary>"Bu akşam 2 saatim var", "hafta sonu farklı bir şey" gibi bağlamsal öneri isteği.</summary>
public sealed record SuggestQuestsCommand(int? AvailableMinutes, CostBand? MaxCost) : ICommand<QuestListDto>;

public sealed class SuggestQuestsCommandValidator : AbstractValidator<SuggestQuestsCommand>
{
    public SuggestQuestsCommandValidator()
    {
        RuleFor(x => x.AvailableMinutes).InclusiveBetween(5, 24 * 60);
        RuleFor(x => x.MaxCost).IsInEnum();
    }
}

internal sealed class SuggestQuestsCommandHandler(QuestOfferService offers, IUserContext user)
    : ICommandHandler<SuggestQuestsCommand, QuestListDto>
{
    public async Task<Result<QuestListDto>> Handle(SuggestQuestsCommand command, CancellationToken cancellationToken)
    {
        var result = await offers.SuggestAsync(user.UserId, command.AvailableMinutes, command.MaxCost, cancellationToken);
        return result.Succeeded
            ? Result<QuestListDto>.Ok(new QuestListDto(result.Data!.Date, result.Data.Quests.ToDtos(), result.Data.Message))
            : Result<QuestListDto>.Fail(result.Errors);
    }
}
