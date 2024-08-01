using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Items;
public class ItemMoveRequested : CancellablePlayerEvent
{
    public Page OldPage { get; }
    public Page NewPage { get; set; }
    public byte OldX { get; }
    public byte NewX { get; set; }
    public byte OldY { get; }
    public byte NewY { get; set; }
    public byte OldRotation { get; }
    public byte NewRotation { get; set; }
    public bool IsSwap { get; }
    public ItemJar? Jar { get; }
    public ItemJar? SwappingJar { get; }
    public ItemMoveRequested(UCPlayer player, Page oldPage, Page newPage, byte oldX, byte newX, byte oldY, byte newY, byte newRotation, bool isSwap, ItemJar? jar, ItemJar? swapping) : base(player)
    {
        if (jar != null)
        {
            Jar = jar;
            OldRotation = jar.rot;
        }
        SwappingJar = swapping;
        OldPage = oldPage;
        NewPage = newPage;
        OldX = oldX;
        NewX = newX;
        OldY = oldY;
        NewY = newY;
        NewRotation = newRotation;
        IsSwap = isSwap;
    }
}
