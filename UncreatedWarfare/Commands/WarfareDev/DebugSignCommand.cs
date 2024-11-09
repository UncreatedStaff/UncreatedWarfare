using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Commands.Syntax;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Commands;

[Command("sign"), SubCommandOf(typeof(WarfareDevCommand))]
internal class DebugSignCommand : IExecutableCommand
{
    public CommandContext Context { get; set; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop) || drop.interactable is not InteractableSign sign)
            throw Context.ReplyString("Must be looking at a sign.");

        if (string.IsNullOrEmpty(sign.text))
            throw Context.ReplyString("This sign has no text on it.");

        Context.ReplyString($"Sign text: \"{TranslationFormattingUtility.Colorize(sign.text, SyntaxColorPalette.GetColor([ typeof(string) ]))}\". This is also in the terminal.");

        WarfareLoggerProvider.WriteToLogRaw(LogLevel.Information, sign.text, null);

        return UniTask.CompletedTask;
    }
}