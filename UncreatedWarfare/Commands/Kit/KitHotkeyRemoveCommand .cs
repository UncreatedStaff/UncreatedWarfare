using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("remove", "delete", "cancel"), SubCommandOf(typeof(KitHotkeyCommand))]
internal class KitHotkeyRemoveCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitHotkeyRemoveCommand(TranslationInjection<KitCommandTranslations> translations, KitManager kitManager)
    {
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(Context.ArgumentCount - 1, out byte slot))
        {
            throw Context.SendHelp();
        }

        if (!KitEx.ValidSlot(slot))
        {
            throw Context.Reply(_translations.KitHotkeyInvalidSlot);
        }

        await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Kit? kit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);

            if (kit == null)
            {
                throw Context.Reply(_translations.KitHotkeyNoKit);
            }

            bool removed = await _kitManager.RemoveHotkey(kit.PrimaryKey, Context.CallerId.m_SteamID, slot, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (!removed)
            {
                throw Context.Reply(_translations.KitHotkeyNotFound, slot, kit);
            }

            byte index = KitEx.GetHotkeyIndex(slot);
            Context.Player.UnturnedPlayer.equipment.ServerClearItemHotkey(index);
            Context.Reply(_translations.KitHotkeyUnbound, slot, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}