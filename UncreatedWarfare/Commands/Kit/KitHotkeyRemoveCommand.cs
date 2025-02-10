using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Translations;
using Z.EntityFramework.Plus;

namespace Uncreated.Warfare.Commands;

[Command("remove", "delete", "cancel"), SubCommandOf(typeof(KitHotkeyCommand))]
internal sealed class KitHotkeyRemoveCommand : IExecutableCommand
{
    private readonly IKitsDbContext _dbContext;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitHotkeyRemoveCommand(TranslationInjection<KitCommandTranslations> translations, IKitsDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(Context.ArgumentCount - 1, out byte slot))
        {
            throw Context.SendHelp();
        }

        if (!KitItemUtility.ValidSlot(slot))
        {
            throw Context.Reply(_translations.KitHotkeyInvalidSlot);
        }

        Kit? activeKit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Translations, token).ConfigureAwait(false);
        if (activeKit == null)
        {
            throw Context.Reply(_translations.KitHotkeyNoKit);
        }

        uint kitId = activeKit.Key;
        int removed = await _dbContext.KitHotkeys
            .Where(x => x.KitId == kitId && x.Slot == slot)
            .DeleteAsync(token)
            .ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        HotkeyPlayerComponent hotkeys = Context.Player.Component<HotkeyPlayerComponent>();
        hotkeys.HotkeyBindings?.RemoveAll(x => x.KitId == kitId && x.Slot == slot);

        if (removed == 0)
        {
            throw Context.Reply(_translations.KitHotkeyNotFound, slot, activeKit);
        }

        byte index = KitItemUtility.GetHotkeyIndex(slot);
        Context.Player.UnturnedPlayer.equipment.ServerClearItemHotkey(index);
        Context.Reply(_translations.KitHotkeyUnbound, slot, activeKit);
    }
}