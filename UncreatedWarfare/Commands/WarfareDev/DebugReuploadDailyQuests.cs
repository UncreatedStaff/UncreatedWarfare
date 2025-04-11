using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Quests.Daily;

namespace Uncreated.Warfare.Commands;

[Command("reuploadquests"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugReuploadDailyQuests : IExecutableCommand
{
    private readonly DailyQuestService _dailyQuestService;
    public required CommandContext Context { get; init; }

    public DebugReuploadDailyQuests(DailyQuestService dailyQuestService)
    {
        _dailyQuestService = dailyQuestService;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        DailyQuestDay[]? days = _dailyQuestService.Days;
        if (days == null)
            throw Context.ReplyString("No generated quests.");

        if (await _dailyQuestService.ReuploadMod(true, days, token))
        {
            Context.ReplyString("Successfully uploaded mod.");
        }
        else
        {
            Context.ReplyString("Failed to upload mod.");
        }
    }
}