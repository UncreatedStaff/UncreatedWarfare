using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class ItemDropRequested : BreakablePlayerEvent
{
    private readonly Item _item;
    public Item Item => _item;
    public ItemDropRequested(UCPlayer player, Item item, bool shouldAllow) : base(player)
    {
        this._item = Item;
        if (!shouldAllow) Break();
    }
}