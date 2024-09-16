using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("confirm", "c"), HideFromHelp]
[Priority(-1)]
public class ConfirmCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.Defer();
        return default;
    }
}

[Command("deny"), HideFromHelp]
[Priority(-1)]
public class DenyCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.Defer();
        return default;
    }
}