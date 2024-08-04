using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Layouts;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitLayouts(KitManager manager)
{
    public KitManager Manager { get; } = manager;
    internal void TryReverseLayoutTransformations(UCPlayer player, IKitItem[] kitItems, uint kit)
    {
        ThreadUtil.assertIsGameThread();
        if (player.LayoutTransformations is not { Count: > 0 })
            return;

        for (int i = 0; i < player.LayoutTransformations.Count; ++i)
        {
            LayoutTransformation t = player.LayoutTransformations[i];
            if (t.Kit != kit)
                continue;

            ReverseLayoutTransformation(t, player, kitItems, kit, ref i);
            if (i < 0) i = 0;
        }
    }
    private void ReverseLayoutTransformation(LayoutTransformation transformation, UCPlayer player, IKitItem[] kitItems, uint kit, ref int i)
    {
        if (player.LayoutTransformations == null)
            return;
        PlayerInventory inv = player.Player.inventory;
        SDG.Unturned.Items page = inv.items[(int)transformation.NewPage];
        ItemJar? current = page.getItem(page.getIndex(transformation.NewX, transformation.NewY));
        if (current == null || current.item?.GetAsset() is not { } asset1)
        {
            L.LogDebug("Current item not in inventory.");
            return;
        }
        IPageKitItem? original = (IPageKitItem?)kitItems.FirstOrDefault(x => x is IPageKitItem jar && jar.X == transformation.OldX && jar.Y == transformation.OldY && jar.Page == transformation.OldPage);
        if (original == null)
        {
            L.LogDebug("Transformation was not in original kit items.");
            return;
        }
        page = inv.items[(int)transformation.OldPage];
        int ct = page.getItemCount();
        for (int i2 = 0; i2 < ct; ++i2)
        {
            ItemJar jar = page.getItem((byte)i2);
            if (!ItemUtility.IsOverlapping(original.X, original.Y, asset1.size_x, asset1.size_y, jar.x, jar.y, jar.size_x, jar.size_y, original.Rotation, jar.rot))
                continue;

            L.LogDebug($"Found reverse collision at {transformation.OldPage}, ({jar.x}, {jar.y}) @ rot {jar.rot}.");
            if (jar == current)
            {
                L.LogDebug(" Collision was same item.");
                continue;
            }
            int index = player.LayoutTransformations.FindIndex(x =>
                x.Kit == kit && x.NewX == jar.x && x.NewY == jar.y &&
                x.NewPage == transformation.OldPage);
            if (index < 0)
            {
                L.LogDebug(" Unable to recursively move back.");
                return;
            }
            LayoutTransformation lt = player.LayoutTransformations[index];
            player.LayoutTransformations.RemoveAtFast(index);
            if (i <= index)
                --i;
            ReverseLayoutTransformation(lt, player, kitItems, kit, ref i);
            break;
        }
        inv.ReceiveDragItem((byte)transformation.NewPage, current.x, current.y, (byte)original.Page, original.X, original.Y, original.Rotation);
        L.LogDebug($"Reversing {transformation.NewPage}, ({current.x}, {current.y}) to {original.Page}, ({original.X}, {original.Y}) @ rot {original.Rotation}.");
    }
    public List<LayoutTransformation> GetLayoutTransformations(UCPlayer player, uint kit)
    {
        ThreadUtil.assertIsGameThread();
        List<LayoutTransformation> output = new List<LayoutTransformation>(player.ItemTransformations.Count);
        SDG.Unturned.Items[] p = player.Player.inventory.items;
        for (int i = 0; i < player.ItemTransformations.Count; i++)
        {
            ItemTransformation transformation = player.ItemTransformations[i];
            SDG.Unturned.Items upage = p[(int)transformation.NewPage];
            ItemJar? jar = upage.getItem(upage.getIndex(transformation.NewX, transformation.NewY));
            if (jar != null && jar.item == transformation.Item)
                output.Add(new LayoutTransformation(transformation.OldPage, transformation.NewPage, transformation.OldX, transformation.OldY, jar.x, jar.y, jar.rot, kit, new KitLayoutTransformation
                {
                    KitId = kit,
                    NewPage = transformation.NewPage,
                    NewX = jar.x,
                    NewY = jar.y,
                    NewRotation = jar.rot,
                    OldPage = transformation.OldPage,
                    OldX = transformation.OldX,
                    OldY = transformation.OldY,
                    Steam64 = player.Steam64
                }));
            else
                L.LogDebug($"Unable to convert ItemTransformation to LayoutTransformation: {transformation.OldPage} -> {transformation.NewPage}, ({transformation.OldX} -> {transformation.NewX}, {transformation.OldY} -> {transformation.NewY}).");
        }

        return output;
    }
}
