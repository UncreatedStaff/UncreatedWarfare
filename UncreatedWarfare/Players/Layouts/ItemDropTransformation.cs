using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Players.Layouts;
public readonly struct ItemDropTransformation
{
    public readonly Page OldPage;
    public readonly byte OldX;
    public readonly byte OldY;
    public readonly Item Item;
    public ItemDropTransformation(Page oldPage, byte oldX, byte oldY, Item item)
    {
        OldPage = oldPage;
        OldX = oldX;
        OldY = oldY;
        Item = item;
    }
}
