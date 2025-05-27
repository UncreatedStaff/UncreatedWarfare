using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Steam;

namespace Uncreated.Warfare.Commands;

[Command("steamguard"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSteamGuardCode : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgsExact(1);

        WorkshopUploader.SteamCode = Context.Get(0)!;
        Context.ReplyString("Attempting to supply Steam Guard code...");
        return UniTask.CompletedTask;
    }
}