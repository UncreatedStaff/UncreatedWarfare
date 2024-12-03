using System.Collections.Generic;
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
    public CommandContext Context { get; set; }

    public KitFavoriteCommand(TranslationInjection<KitCommandTranslations> translations, SignInstancer signs, KitManager kitManager)
    {
        _signs = signs;
        _kitManager = kitManager;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        // ReSharper disable InconsistentlySynchronizedField

        Context.AssertRanByPlayer();

        string? kitId = null;
        Kit? kit = null;
        bool signLoadout = false;

        // kit favorite [kit id or sign]
        if (Context.HasArgs(1))
        {
            kitId = Context.Get(0);
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade)
                 && barricade.interactable is not InteractableSign
                 && _signs.GetSignProvider(barricade) is KitSignInstanceProvider signData)
        {
            if (signData.LoadoutNumber > 0)
            {
                kitId = LoadoutIdHelper.GetLoadoutSignDisplayText(signData.LoadoutNumber);
                kit = await _kitManager.Loadouts.GetLoadout(Context.CallerId, signData.LoadoutNumber, token);
                signLoadout = true;
            }
            else
                kitId = signData.KitId;
        }

        if (kitId == null || signLoadout && kit == null)
        {
            throw Context.Reply(_translations.KitOperationNoTarget);
        }

        kit ??= await _kitManager.FindKit(kitId, token, exactMatchOnly: false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        await UniTask.SwitchToMainThread(token);

        KitPlayerComponent comp = Context.Player.Component<KitPlayerComponent>();
        lock (comp)
        {
            if (comp.FavoritedKits != null && comp.FavoritedKits.Contains(kit.PrimaryKey))
            {
                throw Context.Reply(_translations.KitFavoriteAlreadyFavorited, kit);
            }

            (comp.FavoritedKits ??= new List<uint>(8)).Add(kit.PrimaryKey);
            comp.FavoritesDirty = true;
        }

        Context.Reply(_translations.KitFavorited, kit);


        // ReSharper restore InconsistentlySynchronizedField
    }
}
