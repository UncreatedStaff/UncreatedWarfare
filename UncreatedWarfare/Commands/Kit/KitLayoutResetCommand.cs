using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("reset", "delete", "cancel", "remove"), SubCommandOf(typeof(KitLayoutCommand))]
internal class KitLayoutResetCommand : ICommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitLayoutResetCommand(TranslationInjection<KitCommandTranslations> translations, KitManager kitManager)
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

            if (kit.Items != null)
            {
                await UniTask.SwitchToMainThread(token);
                _kitManager.Layouts.TryReverseLayoutTransformations(Context.Player, kit.Items, kit.PrimaryKey);
            }

            await _kitManager.ResetLayout(Context.Player, kit.PrimaryKey, false, token);
            await UniTask.SwitchToMainThread(token);
            throw Context.Reply(_translations.KitLayoutReset, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}