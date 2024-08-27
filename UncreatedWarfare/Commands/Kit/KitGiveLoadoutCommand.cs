using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("loadout", "l"), SubCommandOf(typeof(KitGiveCommand))]
internal class KitGiveLoadoutCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    public CommandContext Context { get; set; }

    public KitGiveLoadoutCommand(TranslationInjection<KitCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out Class @class))
        {
            if (!Context.HasArgs(1))
                throw Context.SendHelp();

            throw Context.Reply(_translations.ClassNotFound);
        }

        if (!Context.TryGet(1, out _, out WarfarePlayer? player, true) || player == null)
            player = Context.Player;

        IKitItem[] items = KitDefaults.GetDefaultLoadoutItems(@class);

        ItemUtility.GiveItems(player, items, true);

        Context.Reply(_translations.RequestDefaultLoadoutGiven, @class);
        return UniTask.CompletedTask;
    }
}