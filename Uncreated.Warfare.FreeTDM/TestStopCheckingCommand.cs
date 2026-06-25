using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

[Command("ftdmborder"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class TestStopCheckingCommand : IExecutableCommand
{
    public static bool StopChecking = false;

    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        StopChecking = !StopChecking;
        Context.ReplyString(StopChecking ? "Not checking anymore" : "Checking now.");

        return UniTask.CompletedTask;
    }
}