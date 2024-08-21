using Uncreated.Framework.UI;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("unfavorite", "unfavourite", "unfavour", "unfavor", "unfav", "unstar"), SubCommandOf(typeof(KitCommand))]
internal class KitUnfavoriteCommand : IExecutableCommand
{
    private readonly SignInstancer _signs;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    public CommandContext Context { get; set; }

    public KitUnfavoriteCommand(TranslationInjection<KitCommandTranslations> translations, SignInstancer signs, KitManager kitManager)
    {
        _signs = signs;
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        string? kitId = null;

        // kit favorite [kit id or sign]
        if (Context.HasArgs(1))
        {
            kitId = Context.Get(0);
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade)
                 && barricade.interactable is not InteractableSign
                 && _signs.GetSignProvider(barricade) is KitSignInstanceProvider signData)
        {
            kitId = signData.LoadoutNumber > 0
                ? KitEx.GetLoadoutName(Context.CallerId.m_SteamID, signData.LoadoutNumber)
                : signData.KitId;
        }

        if (kitId == null)
        {
            throw Context.Reply(_translations.KitOperationNoTarget);
        }

        Kit? kit = await _kitManager.FindKit(kitId, token, exactMatchOnly: false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);
            
            if (!_kitManager.IsFavoritedQuick(kit.PrimaryKey, Context.Player))
            {
                throw Context.Reply(_translations.KitFavoriteAlreadyUnfavorited, kit);
            }

            KitMenuUIData? data = UnturnedUIDataSource.GetData<KitMenuUIData>(Context.CallerId, _kitManager.MenuUI.Parent);
            if (data?.FavoriteKits?.Remove(kit.PrimaryKey) ?? false)
            {
                data.FavoritesDirty = true;
                await _kitManager.SaveFavorites(Context.Player, data.FavoriteKits, token).ConfigureAwait(false);
            }

            Context.Reply(_translations.KitUnfavorited, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}
