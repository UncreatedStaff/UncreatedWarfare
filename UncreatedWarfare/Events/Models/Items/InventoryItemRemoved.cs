using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Items;

public class InventoryItemRemoved : PlayerEvent
{
    public Page Page { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Index { get; }
    public ItemJar Jar { get; }
    public InventoryItemRemoved(WarfarePlayer player, Page page, byte x, byte y, byte index, ItemJar jar)
    {
        Player = player;
        Page = page;
        X = x;
        Y = y;
        Index = index;
        Jar = jar;
    }
}
