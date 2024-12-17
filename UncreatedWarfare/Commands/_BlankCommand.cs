using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("blank"), SubCommandOf(null), MetadataFile]
internal sealed class BlankCommand : IExecutableCommand
{
    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
    }
}