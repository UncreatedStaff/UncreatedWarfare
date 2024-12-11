using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("shutdown"), Priority(1), HideFromHelp]
internal sealed class ShutdownCommand : IExecutableCommand
{
    private readonly WarfareModule _module;
    public static Coroutine? Messager;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }
    
    public ShutdownCommand(WarfareModule module)
    {
        _module = module;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.ReplyString("Shutting down...");
        await _module.ShutdownAsync(Context.HasArgs(1) ? Context.GetRange(0)! : string.Empty, CancellationToken.None);
    }
}