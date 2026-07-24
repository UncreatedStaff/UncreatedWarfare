using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Cosmetics;

internal class PlayerOptionCosmeticItemProvider : ICosmeticItemProvider
{
    private readonly AssetRedirectService _redirectService;

    bool ICosmeticItemProvider.IsEnabled => true;
    bool ICosmeticItemProvider.PlayerAgnostic => false; // TODO set to true after testing

    public PlayerOptionCosmeticItemProvider(AssetRedirectService redirectService)
    {
        _redirectService = redirectService;
    }


    public ItemClothingAsset? Resolve(WarfarePlayer? player, WarfarePlayer onPlayer, in ClothingItem slot, Kit kit, ref byte quality, ref byte[] state)
    {
        if (slot.Asset == null)
        {
            return null;
        }

        ClothingType type = slot.Type;
        RedirectType redirect = (RedirectType)slot.Type;

        IClothingItem? clothingItem = kit.GetClothingItem(type);

        string? variant = null;
        if (clothingItem is IRedirectedItem redir)
        {
            variant = redir.Variant;
        }

        ItemAsset? asset = _redirectService.ResolveRedirect(redirect, variant ?? string.Empty, onPlayer.Team.Faction, onPlayer.Team, out state, out _);
        quality = 100;
        return asset as ItemClothingAsset;
    }

    public bool ShouldKitCosmeticsBeInstanced(Kit kit)
    {
        return kit.Type != KitType.Public;
    }
}
