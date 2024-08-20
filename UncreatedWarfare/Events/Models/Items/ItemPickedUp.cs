using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Models.Items;

public class ItemPickedUp : PlayerEvent
{
    public Page Page { get; }
    public byte X { get; }
    public byte Y { get; }
    public byte Rotation { get; }
    public byte OldRegionX { get; }
    public byte OldRegionY { get; }
    public uint OldInstanceId { get; }
    public ItemRegion? OldRegion { get; }
    public ItemData? OldDroppedItem { get; }
    public ItemJar? Jar { get; }
    public Item? Item { get; }
    public ItemPickedUp(UCPlayer player, Page page, byte x, byte y, byte rot, byte regX, byte regY, uint instId, ItemRegion? reg, ItemData? data, ItemJar? jar, Item? item) : base(player)
    {
        Page = page;
        X = x;
        Y = y;
        Rotation = rot;
        OldRegionX = regX;
        OldRegionY = regY;
        OldInstanceId = instId;
        OldRegion = reg;
        OldDroppedItem = data;
        Jar = jar;
        Item = item;
    }
}
