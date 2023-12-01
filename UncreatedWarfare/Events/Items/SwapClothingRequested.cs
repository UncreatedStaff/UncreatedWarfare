using SDG.Unturned;
using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Items;
public class SwapClothingRequested : BreakablePlayerEvent
{
    public bool IsRemoving { get; }
    public ClothingType Type { get; }
    public ItemJar? Jar { get; }
    public Page Page { get; }
    public byte X { get; }
    public byte Y { get; }
    public SwapClothingRequested(UCPlayer player, ClothingType type, ItemJar? item, Page page, byte x, byte y) : base(player)
    {
        IsRemoving = (byte)page == byte.MaxValue;
        Jar = item;
        Type = type;
        Page = page;
        X = x;
        Y = y;
    }
}
