using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("blank"), SubCommandOf(null), MetadataFile]
internal sealed class BlankCommand : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
    }
}