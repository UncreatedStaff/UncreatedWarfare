using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;

namespace Uncreated.Warfare.Players.Layouts;
public readonly struct LayoutTransformation
{
    public readonly uint Kit;
    public readonly Page OldPage;
    public readonly Page NewPage;
    public readonly byte OldX;
    public readonly byte OldY;
    public readonly byte NewX;
    public readonly byte NewY;
    public readonly byte NewRotation;
    public readonly KitLayoutTransformation Model;
    public LayoutTransformation(Page oldPage, Page newPage, byte oldX, byte oldY, byte newX, byte newY, byte newRotation, uint kit, KitLayoutTransformation model)
    {
        OldPage = oldPage;
        NewPage = newPage;
        OldX = oldX;
        OldY = oldY;
        NewX = newX;
        NewY = newY;
        Kit = kit;
        NewRotation = newRotation;
        Model = model;
    }
}