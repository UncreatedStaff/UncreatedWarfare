using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("unfavorite", "unfavourite", "unfavour", "unfavor", "unfav", "unstar"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitUnfavoriteCommand : IExecutableCommand
{
    private readonly SignInstancer _signs;
    private readonly LoadoutService _loadoutService;
    private readonly IKitFavoriteService _kitFavoriteService;
    private readonly IKitDataStore _kitDataStore;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitUnfavoriteCommand(
        TranslationInjection<KitCommandTranslations> translations,
        SignInstancer signs,
        LoadoutService loadoutService,
        IKitFavoriteService kitFavoriteService,
        IKitDataStore kitDataStore)
    {
        _signs = signs;
        _loadoutService = loadoutService;
        _kitFavoriteService = kitFavoriteService;
        _kitDataStore = kitDataStore;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        string? kitId = null;
        Kit? kit = null;
        bool signLoadout = false;

        // kit unfavorite [kit id or sign]
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
                kit = await _loadoutService.GetLoadoutFromNumber(Context.CallerId, signData.LoadoutNumber, KitInclude.Translations, token).ConfigureAwait(false);
                signLoadout = true;
            }
            else
                kitId = signData.KitId;
        }

        if (kitId == null || signLoadout && kit == null)
        {
            throw Context.Reply(_translations.KitOperationNoTarget);
        }

        kit ??= await _kitDataStore.QueryKitAsync(kitId, KitInclude.Translations, token).ConfigureAwait(false);
        if (kit == null)
        {
            throw Context.Reply(_translations.KitNotFound, kitId);
        }

        await _kitFavoriteService.RemoveFavorite(Context.CallerId, kit.Key, token).ConfigureAwait(false);
        Context.Reply(_translations.KitUnfavorited, kit);
    }
}