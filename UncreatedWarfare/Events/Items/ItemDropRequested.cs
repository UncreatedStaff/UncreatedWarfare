using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Events.Barricades;
public class ItemDropRequested : BreakablePlayerEvent
{
    private readonly Item _item;
    private readonly ItemJar _itemJar;
    private readonly byte _page;
    private readonly byte _index;
    public Item Item => _item;
    public ItemJar ItemJar => _itemJar;
    public byte Page => _page;
    public byte X => _itemJar.x;
    public byte Y => _itemJar.y;
    public byte Index => _index;
    public ItemDropRequested(UCPlayer player, Item item, ItemJar jar, byte page, byte index, bool shouldAllow) : base(player)
    {
        this._item = item;
        this._itemJar = jar;
        this._page = page;
        this._index = index;
        if (!shouldAllow) Break();
    }
}