using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("favorite", "favourite", "favour", "favor", "fav", "star"), SubCommandOf(typeof(KitCommand))]
internal class KitFavoriteCommand : IExecutableCommand
{
    private readonly SignInstancer _signs;
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly IServiceProvider _serviceProvider;
    public CommandContext Context { get; set; }

    public KitFavoriteCommand(TranslationInjection<KitCommandTranslations> translations, SignInstancer signs, KitManager kitManager, IServiceProvider serviceProvider)
    {
        _signs = signs;
        _kitManager = kitManager;
        _translations = translations.Value;
        _serviceProvider = serviceProvider;
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
            
            if (_kitManager.IsFavoritedQuick(kit.PrimaryKey, Context.Player))
            {
                throw Context.Reply(_translations.KitFavoriteAlreadyFavorited, kit);
            }

            KitMenuUIData? data = UnturnedUIDataSource.GetData<KitMenuUIData>(Context.CallerId, _kitManager.MenuUI.Parent);
            if (data == null)
            {
                data = new KitMenuUIData(_kitManager.MenuUI, _kitManager.MenuUI.Parent, Context.Player, _serviceProvider);
                UnturnedUIDataSource.AddData(data);
            }

            (data.FavoriteKits ??= new List<uint>(8)).Add(kit.PrimaryKey);
            data.FavoritesDirty = true;

            await _kitManager.SaveFavorites(Context.Player, data.FavoriteKits, token).ConfigureAwait(false);

            Context.Reply(_translations.KitFavorited, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}
