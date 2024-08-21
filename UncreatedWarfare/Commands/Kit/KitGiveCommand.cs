using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("give", "g"), SubCommandOf(typeof(KitCommand))]
internal class KitGiveCommand : IExecutableCommand
{
    private readonly SignInstancer _signs;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitGiveCommand(TranslationInjection<KitCommandTranslations> translations, SignInstancer signs, KitManager kitManager)
    {
        _signs = signs;
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        string? kitId = null;
        WarfarePlayer? player = null;
        BarricadeDrop? barricade = null;
        bool kitIdCouldBePlayerName = false;
         
        // kit give [kit (or target sign)] [player]
        if (Context.HasArgs(2))
        {
            if (!Context.TryGet(1, out _, out player, remainder: true) || player == null)
            {
                throw Context.SendPlayerNotFound();
            }

            kitId = Context.Get(0);
        }
        else if (Context.HasArgs(1))
        {
            if (Context.TryGetBarricadeTarget(out barricade) && barricade.interactable is InteractableSign)
            {
                if (!Context.TryGet(1, out _, out player, remainder: true) || player == null)
                {
                    kitId = Context.Get(1);
                    barricade = null;
                    kitIdCouldBePlayerName = true;
                }
            }
            else
            {
                kitId = Context.Get(0);
            }
        }
        else
        {
            if (!Context.TryGetBarricadeTarget(out barricade) || barricade.interactable is not InteractableSign)
            {
                throw Context.Reply(_translations.KitOperationNoTarget);
            }
        }

        if (barricade != null && _signs.GetSignProvider(barricade) is KitSignInstanceProvider signData)
        {
            if (signData.LoadoutNumber > 0)
            {
                Context.AssertRanByPlayer();
                kitId = KitEx.GetLoadoutName(Context.CallerId.m_SteamID, signData.LoadoutNumber);
            }
            else
            {
                kitId = signData.KitId;
            }
        }

        if (kitId == null)
        {
            throw Context.Reply(_translations.KitOperationNoTarget);
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, exactMatchOnly: false);
        if (kit == null)
        {
            if (kitIdCouldBePlayerName)
                throw Context.SendPlayerNotFound();

            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        if (Equals(Context.Player, player))
        {
            player = null;
        }
        
        if (player == null)
        {
            Context.AssertRanByPlayer();
        }

        await _kitManager.Requests.GiveKit(player ?? Context.Player, kit, manual: true, tip: true, token).ConfigureAwait(false);
        await UniTask.SwitchToMainThread(token);
        Context.LogAction(ActionLogType.GiveKit, kit.InternalName);

        if (player == null)
        {
            Context.Reply(_translations.KitGiveSuccess, kit);
        }
        else
        {
            Context.Reply(_translations.KitGiveSuccessToPlayer, kit, player);
        }
    }
}
