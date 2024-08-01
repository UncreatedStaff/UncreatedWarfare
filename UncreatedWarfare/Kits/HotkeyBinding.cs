using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Kits;
public struct HotkeyBinding
{
    public uint Kit { get; set; }
    public byte Slot { get; set; }
    public IPageKitItem Item { get; set; }
    public KitHotkey Model { get; set; }
    public HotkeyBinding(uint kit, byte slot, IPageKitItem item, KitHotkey model)
    {
        Kit = kit;
        Slot = slot;
        Item = item;
        Model = model;
    }
    public ItemAsset? GetAsset(Kit? kit, ulong team)
    {
        return Item switch
        {
            null => null,
            ISpecificKitItem item => item.Item.GetAsset<ItemAsset>(),
            _ => Item.GetItem(kit, TeamManager.GetFactionSafe(team), out _, out _)
        };
    }
}