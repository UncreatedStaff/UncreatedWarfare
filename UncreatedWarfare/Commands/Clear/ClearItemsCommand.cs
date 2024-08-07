using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("items", "item", "i"), SubCommandOf(typeof(ClearCommand))]
public class ClearItemsCommand : IExecutableCommand
{
    private readonly ClearTranslations _translations;

    public CommandContext Context { get; set; }
    public ClearItemsCommand(TranslationInjection<ClearTranslations> translations)
    {
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(1, out float range))
        {
            ItemUtility.DestroyAllDroppedItems(false);
            Context.LogAction(ActionLogType.ClearItems);
            throw Context.Reply(_translations.ClearItems);
        }

        Context.AssertRanByPlayer();
        ItemUtility.DestroyDroppedItemsInRange(Context.Player.Position, range, false);
        Context.LogAction(ActionLogType.ClearItems, "RANGE: " + range.ToString("F0") + "m");
        Context.Reply(_translations.ClearItemsInRange, range);

        return UniTask.CompletedTask;
    }
}