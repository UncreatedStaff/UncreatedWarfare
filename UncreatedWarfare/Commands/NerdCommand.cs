using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction;

namespace Uncreated.Warfare.Commands;

[Command("nerd"), MetadataFile]
internal sealed class NerdCommand : IExecutableCommand
{
    private readonly NerdService _nerdService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public NerdCommand(NerdService nerdService)
    {
        _nerdService = nerdService;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        /*
                if (!await _nerdService.SetNerdnessAsync(token).ConfigureAwait(false))
                {
                    throw Context.SendNoPermission();
                }
        */
        Context.Defer();
    }
}