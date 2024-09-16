using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("buy"), MetadataFile]
public class BuyCommand : IExecutableCommand
{
    private readonly KitManager _kitManager;
    private readonly SignInstancer _signs;
    private readonly RequestTranslations _translations;
    private readonly KitCommandTranslations _kitTranslations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public BuyCommand(TranslationInjection<RequestTranslations> translations, TranslationInjection<KitCommandTranslations> kitTranslations, KitManager kitManager, SignInstancer signs)
    {
        _kitManager = kitManager;
        _signs = signs;
        _translations = translations.Value;
        _kitTranslations = kitTranslations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop) || drop.interactable is not InteractableSign)
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }

        string? kitId = null;
        if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade)
            && barricade.interactable is not InteractableSign
            && _signs.GetSignProvider(barricade) is KitSignInstanceProvider signData)
        {
            if (signData.LoadoutNumber > 0)
            {
                Kit? loadout = await _kitManager.Loadouts.GetLoadout(Context.CallerId, signData.LoadoutNumber, token);
                throw Context.Reply(loadout == null ? _translations.RequestBuyLoadout : _translations.RequestNotBuyable);
            }

            kitId = signData.KitId;
        }

        Kit? kit = kitId == null ? null : await _kitManager.FindKit(kitId, token, true);
        if (kit == null)
        {
            throw Context.Reply(_kitTranslations.KitNotFound);
        }

        await _kitManager.Requests.BuyKit(Context, kit, drop.model.position, token).ConfigureAwait(false);
    }
}
