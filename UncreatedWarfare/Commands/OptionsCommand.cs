using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.UI;

namespace Uncreated.Warfare.Commands;

[Command("options", "settings", "option", "config", "lang", "language", "culture", "time", "timezone", "tz"), MetadataFile]
internal sealed class OptionsCommand : IExecutableCommand
{
    private readonly OptionsUI _optionsUi;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public OptionsCommand(OptionsUI optionsUi)
    {
        _optionsUi = optionsUi;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        _optionsUi.Open(Context.Player);
        return UniTask.CompletedTask;
    }
}