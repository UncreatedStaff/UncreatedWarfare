using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("save", "confirm", "keep"), SubCommandOf(typeof(KitLayoutCommand))]
internal class KitLayoutSaveCommand : ICommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitLayoutSaveCommand(TranslationInjection<KitCommandTranslations> translations, KitManager kitManager)
    {
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Kit? kit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);

            if (kit == null)
            {
                throw Context.Reply(_translations.KitLayoutNoKit);
            }

            await UniTask.SwitchToMainThread(token);

            await _kitManager.SaveLayout(Context.Player, kit, false, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            Context.Reply(_translations.KitLayoutSaved, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}