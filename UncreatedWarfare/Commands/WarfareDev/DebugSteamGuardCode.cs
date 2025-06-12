using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Steam;

namespace Uncreated.Warfare.Commands;

[Command("steamguard"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSteamGuardCode : IExecutableCommand
{
    private readonly IWorkshopUploader _uploader;

    public required CommandContext Context { get; init; }

    public DebugSteamGuardCode(IWorkshopUploader uploader)
    {
        _uploader = uploader;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgsExact(1);

        Context.ReplyString("Attempting to supply Steam Guard code...");
        _uploader.SteamCode = Context.Get(0)!;
        return UniTask.CompletedTask;
    }
}