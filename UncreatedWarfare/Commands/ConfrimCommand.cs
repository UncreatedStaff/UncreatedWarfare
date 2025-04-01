using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("confirm", "c"), HideFromHelp]
[Priority(-1)]
public sealed class ConfirmCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.Defer();
        return default;
    }
}

[Command("deny"), HideFromHelp]
[Priority(-1)]
public sealed class DenyCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.Defer();
        return default;
    }
}