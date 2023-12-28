using SDG.Unturned;
using System;

namespace Uncreated.Warfare.Events.Items;
public class CraftRequested : BreakablePlayerEvent
{
    private ItemAsset _item;
    private Blueprint _blueprint;
    public ushort ItemId { get; private set; }
    public byte BlueprintIndex { get; private set; }
    public ItemAsset Item
    {
        get => _item;
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            ItemId = value.id;
            _item = value;
        }
    }
    public Blueprint Blueprint
    {
        get => _blueprint;
        set
        {
            if (Item is null)
                throw new InvalidOperationException("Item not found!");
            int index = _item.blueprints.IndexOf(value);
            if (index < 0 || index > byte.MaxValue)
                throw new ArgumentException("Blueprint must be owned by 'Item' (" + _item.itemName + ").");
            _blueprint = value;
            BlueprintIndex = (byte)index;
        }
    }
    public CraftRequested(UCPlayer player, ItemAsset item, Blueprint blueprint, bool shouldAllow) : base(player, shouldAllow)
    {
        Item = item;
        Blueprint = blueprint;
    }
}
