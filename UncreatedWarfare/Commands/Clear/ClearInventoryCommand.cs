using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("inventory", "inv"), SubCommandOf(typeof(ClearCommand))]
public class ClearInventoryCommand : IExecutableCommand
{
    private readonly ClearTranslations _translations;

    public CommandContext Context { get; set; }
    public ClearInventoryCommand(TranslationInjection<ClearTranslations> translations)
    {
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (Context.TryGet(1, out _, out WarfarePlayer? pl) || Context.HasArgs(2))
        {
            // clear inv <player>
            if (pl == null)
                throw Context.Reply(T.PlayerNotFound);
            
            ItemUtility.ClearInventoryAndSlots(pl);

            Context.LogAction(ActionLogType.ClearInventory, "CLEARED INVENTORY OF " + pl.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
            Context.Reply(_translations.ClearInventoryOther, pl);
        }
        else if (!Context.Caller.IsTerminal)
        {
            // clear inv
            ItemUtility.ClearInventoryAndSlots(Context.Player);
            Context.LogAction(ActionLogType.ClearInventory, "CLEARED PERSONAL INVENTORY");
            Context.Reply(_translations.ClearInventorySelf);
        }
        else
        {
            throw Context.Reply(_translations.ClearNoPlayerConsole);
        }

        return UniTask.CompletedTask;
    }
}