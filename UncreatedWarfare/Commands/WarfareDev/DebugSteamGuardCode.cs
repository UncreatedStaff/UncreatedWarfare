using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("steamguard"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSteamGuardCode : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgsExact(1);

        WorkshopUploader.SteamCode = Context.Get(0)!;
        Context.ReplyString("Supplied Steam Guard Code.");
        return UniTask.CompletedTask;
    }
}