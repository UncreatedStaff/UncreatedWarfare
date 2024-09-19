using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory" ])]
public class CraftRequested : CancellablePlayerEvent
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

    [SetsRequiredMembers]
    public CraftRequested(WarfarePlayer player, ItemAsset item, Blueprint blueprint)
    {
        Player = player;
        Item = item;
        Blueprint = blueprint;
    }
}
