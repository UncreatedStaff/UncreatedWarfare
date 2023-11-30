using SDG.Unturned;
using Uncreated.Warfare.Kits.Items;
using UnityEngine;

namespace Uncreated.Warfare.Events.Items;
public class ItemDropped : PlayerEvent
{
    public Item? Item { get; }
    public ItemData? DroppedItem { get; }
    public Page OldPage { get; }
    public byte OldX { get; }
    public byte OldY { get; }
    public byte OldRotation { get; }
    public ItemDropped(UCPlayer player, Item? item, ItemData? data, Page page, byte x, byte y, byte rot) : base(player)
    {
        Item = item;
        DroppedItem = data;
        OldPage = page;
        OldX = x;
        OldY = y;
        OldRotation = rot;
    }
}