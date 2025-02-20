using System;
using System.Runtime.CompilerServices;

namespace Uncreated.Warfare.Events.Models.Items;

[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory", "modify_useable" ])]
public class CraftItemRequested : CancellablePlayerEvent
{
    // ReSharper disable once ReplaceWithFieldKeyword
    private Blueprint _blueprint;
    private ItemAsset _item;

    public ushort ItemId { get; private set; }
    public byte BlueprintIndex { get; private set; }

    /// <summary>
    /// The item that is being crafted.
    /// </summary>
    public ItemAsset Item
    {
        get => _item;
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (value.id == 0)
                throw new ArgumentException("No ID on asset.", nameof(value));

            ItemId = value.id;
            _item = value;
        }
    }

    /// <summary>
    /// The blueprint on <see cref="Item"/> that is being crafted.
    /// </summary>
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

    /// <summary>
    /// If as many items as possible should be crafted instead of just one.
    /// </summary>
    public required bool CraftAll { get; set; }

    [SetsRequiredMembers]
    internal CraftItemRequested(ItemAsset itemAsset, byte blueprintIndex)
    {
        ItemId = itemAsset.id;
        _item = itemAsset;

        _blueprint = itemAsset.blueprints[blueprintIndex];
        BlueprintIndex = blueprintIndex;
    }
}
