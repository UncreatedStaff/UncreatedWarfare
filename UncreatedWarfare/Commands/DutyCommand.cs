using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[SynchronizedCommand, Command("duty", "onduty", "offduty", "d"), MetadataFile]
internal sealed class DutyCommand : IExecutableCommand
{
    private readonly DutyService _dutyService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public DutyCommand(DutyService dutyService)
    {
        _dutyService = dutyService;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!await _dutyService.ToggleDutyStateAsync(Context.CallerId, token).ConfigureAwait(false))
        {
            throw Context.SendNoPermission();
        }

        Context.Defer();
    }
}

public class DutyCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Duty";

    [TranslationData("Sent to a player when they go on duty.", IsPriorityTranslation = false)]
    public readonly Translation DutyOnFeedback = new Translation("<#c6d4b8>You are now <#95ff4a>on duty</color>.");

    [TranslationData("Sent to a player when they go off duty.", IsPriorityTranslation = false)]
    public readonly Translation DutyOffFeedback = new Translation("<#c6d4b8>You are now <#ff8c4a>off duty</color>.");

    [TranslationData("Sent to all players when a player goes on duty (gains permissions).")]
    public readonly Translation<IPlayer> DutyOnBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#95ff4a>on duty</color>.", arg0Fmt: WarfarePlayer.FormatDisplayOrPlayerName);

    [TranslationData("Sent to all players when a player goes off duty (loses permissions).")]
    public readonly Translation<IPlayer> DutyOffBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#ff8c4a>off duty</color>.", arg0Fmt: WarfarePlayer.FormatDisplayOrPlayerName);
}