using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Players.Layouts;
public readonly struct ItemTransformation
{
    public readonly Page OldPage;
    public readonly Page NewPage;
    public readonly byte OldX;
    public readonly byte OldY;
    public readonly byte NewX;
    public readonly byte NewY;
    public readonly Item Item;
    public ItemTransformation(Page oldPage, Page newPage, byte oldX, byte oldY, byte newX, byte newY, Item item)
    {
        OldPage = oldPage;
        NewPage = newPage;
        OldX = oldX;
        OldY = oldY;
        NewX = newX;
        NewY = newY;
        Item = item;
    }
}