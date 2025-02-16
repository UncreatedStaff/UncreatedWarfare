using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Players.ItemTracking;

/// <summary>
/// Stores a record of where a dropped item used to be.
/// </summary>
public readonly struct ItemDropTransformation
{
    public readonly Page OldPage;
    public readonly byte OldX;
    public readonly byte OldY;
    public readonly Page DroppedFromPage;
    public readonly byte DroppedFromX;
    public readonly byte DroppedFromY;
    public readonly byte DroppedFromRotation;
    public readonly Item Item;

    public ItemDropTransformation(Page oldPage, byte oldX, byte oldY, Item item, Page droppedFromPage = (Page)byte.MaxValue, byte droppedFromX = byte.MaxValue, byte droppedFromY = byte.MaxValue, byte droppedFromRotation = byte.MaxValue)
    {
        OldPage = oldPage;
        OldX = oldX;
        OldY = oldY;
        Item = item;
        DroppedFromPage = droppedFromPage;
        DroppedFromX = droppedFromX;
        DroppedFromY = droppedFromY;
        DroppedFromRotation = droppedFromRotation;
    }
}
