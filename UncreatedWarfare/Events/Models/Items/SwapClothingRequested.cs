using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Models.Items;

/// <summary>
/// Invoked when a player tries to swap their clothes to another item or remove clothes completely.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.PerPlayer, SynchronizedModelTags = [ "modify_inventory" ])]
public class SwapClothingRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The clothing slot being modified.
    /// </summary>
    public required ClothingType Type { get; init; }

    /// <summary>
    /// The asset of the current clothing equipped to the slot, if any.
    /// </summary>
    public required ItemClothingAsset? CurrentClothing { get; init; }

    /// <summary>
    /// The state of the current clothing equipped to the slot, if any.
    /// </summary>
    public required byte[]? CurrentClothingState { get; init; }

    /// <summary>
    /// The quality of the current clothing equipped to the slot, if any.
    /// </summary>
    public required byte CurrentClothingQuality { get; init; }

    /// <summary>
    /// The asset of the item being equipped, if any.
    /// </summary>
    public ItemClothingAsset? EquippingClothing { get; private set; }

    /// <summary>
    /// The jar of the item being equipped, if any.
    /// </summary>
    public ItemJar? EquippingJar { get; private set; }

    /// <summary>
    /// The item being equipped, if any.
    /// </summary>
    public Item? EquippingItem { get; private set; }

    /// <summary>
    /// If clothes are being removed.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public bool IsRemoving { get; private set; }

    /// <summary>
    /// The page of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public Page EquippingPage { get; private set; }

    /// <summary>
    /// The X position of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public byte EquippingX { get; private set; }

    /// <summary>
    /// The Y position of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public byte EquippingY { get; private set; }

    /// <summary>
    /// The page of the original location of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public Page EquippingOriginalPage { get; private set; }

    /// <summary>
    /// The X position of the original location of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public byte EquippingOriginalX { get; private set; }

    /// <summary>
    /// The Y position of the original location of the item being equipped, if any.
    /// </summary>
    /// <remarks>This can be changed using <see cref="TryChangeItem"/> or <see cref="DequipItem"/>.</remarks>
    public byte EquippingOriginalY { get; private set; }

    public SwapClothingRequested(Page page, byte x, byte y, ItemJar? jar, ItemClothingAsset? clothing)
    {
        EquippingPage = page;
        EquippingX = x;
        EquippingY = y;
        EquippingJar = jar;
        EquippingItem = jar?.item;
        EquippingClothing = clothing;
        IsRemoving = page == (Page)byte.MaxValue;

        UpdateOriginalPositions();
    }

    /// <summary>
    /// Set the item to be equipped as none, effectively dequipping the existing clothing.
    /// </summary>
    public void DequipItem()
    {
        EquippingPage = (Page)byte.MaxValue;
        EquippingX = byte.MaxValue;
        EquippingY = byte.MaxValue;
        EquippingJar = null;
        EquippingItem = null;
        EquippingClothing = null;
        IsRemoving = true;

        UpdateOriginalPositions();
    }

    /// <summary>
    /// Set the item that is being equipped.
    /// </summary>
    /// <returns><see langword="true"/> if the selected item exists and it's type matches the clothing type required, otherwise <see langword="false"/>.</returns>
    public bool TryChangeItem(Page page, byte x, byte y)
    {
        GameThread.AssertCurrent();

        PlayerInventory inventory = Player.UnturnedPlayer.inventory;
        byte index = inventory.getIndex((byte)page, x, y);
        if (index == byte.MaxValue)
        {
            return false;
        }

        ItemJar jar = inventory.getItem((byte)page, index);
        ItemClothingAsset? asset = jar.GetAsset<ItemClothingAsset>();
        if (asset == null || asset.type != Type.GetItemType())
        {
            return false;
        }

        EquippingPage = page;
        EquippingX = x;
        EquippingY = y;
        EquippingJar = jar;
        EquippingItem = jar.item;
        EquippingClothing = asset;
        IsRemoving = false;

        UpdateOriginalPositions();
        return true;
    }

    private void UpdateOriginalPositions()
    {
        if (EquippingPage == (Page)byte.MaxValue)
        {
            EquippingOriginalPage = (Page)byte.MaxValue;
            EquippingOriginalX = byte.MaxValue;
            EquippingOriginalY = byte.MaxValue;
            return;
        }

        Player.Component<ItemTrackingPlayerComponent>().GetOriginalItemPosition(EquippingPage, EquippingX, EquippingY, out Page originalPage, out byte originalX, out byte originalY);
        EquippingOriginalPage = originalPage;
        EquippingOriginalX = originalX;
        EquippingOriginalY = originalY;
    }
}
