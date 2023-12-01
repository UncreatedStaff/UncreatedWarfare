using SDG.Unturned;

namespace Uncreated.Warfare.Events.Items;
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
        _item = item;
        _itemJar = jar;
        _page = page;
        _index = index;
        if (!shouldAllow) Break();
    }
}