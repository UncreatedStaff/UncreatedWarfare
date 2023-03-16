using SDG.Unturned;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Items;
public class InventoryItemRemoved : PlayerEvent
{
    public Page Page { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Index { get; }
    public ItemJar Jar { get; }
    public InventoryItemRemoved(UCPlayer player, Page page, byte x, byte y, byte index, ItemJar jar) : base(player)
    {
        Page = page;
        X = x;
        Y = y;
        Index = index;
        Jar = jar;
    }
}
