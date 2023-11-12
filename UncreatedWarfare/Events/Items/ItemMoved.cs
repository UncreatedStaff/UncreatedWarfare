using SDG.Unturned;
using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Events.Items;
public class ItemMoved : PlayerEvent
{
    public Page OldPage { get; }
    public Page NewPage { get; }
    public byte OldX { get; }
    public byte NewX { get; }
    public byte OldY { get; }
    public byte NewY { get; }
    public byte NewRotation { get; }
    /// <remarks>Only valid when <see cref="IsSwap"/> is <see langword="true"/>.</remarks>
    public byte OldRotation { get; }
    public bool IsSwap { get; }
    public ItemJar? Jar { get; }
    public ItemJar? SwappedJar { get; }
    public ItemMoved(UCPlayer player, Page oldPage, Page newPage, byte oldX, byte newX, byte oldY, byte newY, byte oldRotation, byte newRotation, bool isSwap, ItemJar? jar, ItemJar? swapped) : base(player)
    {
        Jar = jar;
        SwappedJar = swapped;
        OldPage = oldPage;
        NewPage = newPage;
        OldX = oldX;
        NewX = newX;
        OldY = oldY;
        NewY = newY;
        NewRotation = newRotation;
        OldRotation = oldRotation;
        IsSwap = isSwap;
    }
}
