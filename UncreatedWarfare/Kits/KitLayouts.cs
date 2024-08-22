#if DEBUG
//#define LAYOUT_DEBUG
#endif
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.ItemTracking;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;
public class KitLayouts(KitManager manager, IServiceProvider serviceProvider)
{
#if LAYOUT_DEBUG
    private readonly ILogger<KitLayouts> _logger = serviceProvider.GetRequiredService<ILogger<KitLayouts>>();
#endif
    public KitManager Manager { get; } = manager;
    internal void TryReverseLayoutTransformations(WarfarePlayer player, IKitItem[] kitItems, uint kit)
    {
        GameThread.AssertCurrent();
        ItemTrackingPlayerComponent component = player.Component<ItemTrackingPlayerComponent>();
        if (component.LayoutTransformations is not { Count: > 0 })
            return;

        for (int i = 0; i < component.LayoutTransformations.Count; ++i)
        {
            ItemLayoutTransformationData t = component.LayoutTransformations[i];
            if (t.Kit != kit)
                continue;

            ReverseLayoutTransformation(t, player, kitItems, kit, ref i);
            if (i < 0) i = 0;
        }
    }

    private void ReverseLayoutTransformation(ItemLayoutTransformationData transformation, WarfarePlayer player, IKitItem[] kitItems, uint kit, ref int i)
    {
        ItemTrackingPlayerComponent component = player.Component<ItemTrackingPlayerComponent>();
        if (component.LayoutTransformations == null)
            return;

        PlayerInventory inv = player.UnturnedPlayer.inventory;
        SDG.Unturned.Items page = inv.items[(int)transformation.NewPage];

        ItemJar? current = page.getItem(page.getIndex(transformation.NewX, transformation.NewY));
        if (current?.item?.GetAsset() is not { } asset)
        {
#if LAYOUT_DEBUG
            _logger.LogDebug("Current item ({0}, {1}) not in inventory.", transformation.NewX, transformation.NewY);
#endif
            return;
        }

        IPageKitItem? original = (IPageKitItem?)kitItems.FirstOrDefault(x => x is IPageKitItem jar && jar.X == transformation.OldX && jar.Y == transformation.OldY && jar.Page == transformation.OldPage);
        if (original == null)
        {
#if LAYOUT_DEBUG
            _logger.LogDebug("Current item ({0}, {1}) in inventory but not in kit.", transformation.NewX, transformation.NewY);
#endif
            return;
        }

        page = inv.items[(int)transformation.OldPage];
        int ct = page.getItemCount();
        for (int i2 = 0; i2 < ct; ++i2)
        {
            ItemJar jar = page.getItem((byte)i2);
            if (!ItemUtility.IsOverlapping(original.X, original.Y, asset.size_x, asset.size_y, jar.x, jar.y, jar.size_x, jar.size_y, original.Rotation, jar.rot))
                continue;

#if LAYOUT_DEBUG
            _logger.LogDebug("Found reverse collision at {0}, ({1}, {2}) @ rot {3}.", transformation.OldPage, jar.x, jar.y, jar.rot);
#endif
            if (jar == current)
            {
#if LAYOUT_DEBUG
                _logger.LogDebug(" Collision was same item.");
#endif
                continue;
            }
            int index = component.LayoutTransformations.FindIndex(x =>
                x.Kit == kit && x.NewX == jar.x && x.NewY == jar.y &&
                x.NewPage == transformation.OldPage);
            if (index < 0)
            {
#if LAYOUT_DEBUG
                _logger.LogDebug(" Unable to recursively move back.");
#endif
                return;
            }
            ItemLayoutTransformationData lt = component.LayoutTransformations[index];
            component.LayoutTransformations.RemoveAtFast(index);
            if (i <= index)
                --i;
            ReverseLayoutTransformation(lt, player, kitItems, kit, ref i);
            break;
        }
        inv.ReceiveDragItem((byte)transformation.NewPage, current.x, current.y, (byte)original.Page, original.X, original.Y, original.Rotation);
#if LAYOUT_DEBUG
        _logger.LogDebug(
            "Reversing {0}, ({1}, {2}) to {3}, ({4}, {5}) @ rot {6}.",
            transformation.NewPage,
            current.x,
            current.y,
            original.Page,
            original.X,
            original.Y,
            original.Rotation
        );
#endif
    }

    public List<ItemLayoutTransformationData> GetLayoutTransformations(WarfarePlayer player, uint kit)
    {
        GameThread.AssertCurrent();

        ItemTrackingPlayerComponent component = player.Component<ItemTrackingPlayerComponent>();
        List<ItemLayoutTransformationData> output = new List<ItemLayoutTransformationData>(component.ItemTransformations.Count);
        SDG.Unturned.Items[] p = player.UnturnedPlayer.inventory.items;
        for (int i = 0; i < component.ItemTransformations.Count; i++)
        {
            ItemTransformation transformation = component.ItemTransformations[i];
            SDG.Unturned.Items upage = p[(int)transformation.NewPage];
            ItemJar? jar = upage.getItem(upage.getIndex(transformation.NewX, transformation.NewY));
            if (jar != null && jar.item == transformation.Item)
            {
                output.Add(new ItemLayoutTransformationData(transformation.OldPage, transformation.NewPage, transformation.OldX, transformation.OldY, jar.x, jar.y, jar.rot, kit, new KitLayoutTransformation
                {
                    KitId = kit,
                    NewPage = transformation.NewPage,
                    NewX = jar.x,
                    NewY = jar.y,
                    NewRotation = jar.rot,
                    OldPage = transformation.OldPage,
                    OldX = transformation.OldX,
                    OldY = transformation.OldY,
                    Steam64 = player.Steam64.m_SteamID
                }));
            }
            else
            {
#if LAYOUT_DEBUG
                _logger.LogDebug(
                    "Unable to convert ItemTransformation to LayoutTransformation: {0} -> {1}, ({2} -> {3}, {4} -> {5}).",
                    transformation.OldPage,
                    transformation.NewPage,
                    transformation.OldX,
                    transformation.NewX,
                    transformation.OldY,
                    transformation.NewY
                );
#endif
            }
        }

        return output;
    }
}
