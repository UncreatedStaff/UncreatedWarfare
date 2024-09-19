using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Items;
internal readonly struct SimplifiedItemListEntry
{
    internal readonly ItemAsset? Asset;
    internal readonly string? ClothingSetName;
    internal readonly int Count;
    internal readonly RedirectType RedirectType;
    public SimplifiedItemListEntry(ItemAsset? asset, string? clothingSetName, int count, RedirectType redirectType)
    {
        Asset = asset;
        ClothingSetName = clothingSetName;
        Count = count;
        RedirectType = redirectType;
    }

    internal static List<SimplifiedItemListEntry> GetSimplifiedItemList(Kit kit)
    {
        FactionInfo? faction = null;// todo TeamManager.GetFactionInfo(kit.FactionId);
        List<SimplifiedItemListEntry> groups = new List<SimplifiedItemListEntry>(16);
        List<IKitItem> items = new List<IKitItem>(kit.Items.OrderBy(x => x is not IPageKitItem jar || jar.Page > Page.Secondary));
        items.Sort((a, b) => a.CompareTo(b));
        string? clothingSetName = null;
        for (int i = 0; i < items.Count; ++i)
        {
            IKitItem item = items[i];
            if (item is IAssetRedirectKitItem redir)
            {
                int index2 = groups.FindLastIndex(x => x.RedirectType == redir.RedirectType);
                if (index2 != -1)
                {
                    SimplifiedItemListEntry grp = groups[index2];
                    groups[index2] = new SimplifiedItemListEntry(null, null, grp.Count + 1, grp.RedirectType);
                    continue;
                }
                if (redir.RedirectType <= RedirectType.Glasses)
                {
                    ItemAsset? asset = null;// todo TeamManager.GetRedirectInfo(redir.RedirectType, redir.RedirectVariant ?? string.Empty, faction, null, out _, out _);
                    if (asset != null)
                    {
                        if (redir.RedirectType is RedirectType.Shirt or RedirectType.Pants && clothingSetName == null)
                        {
                            if (asset != null)
                            {
                                int index3 = asset.name.IndexOf(redir.RedirectType == RedirectType.Shirt ? "_Top" : "_Bottom", StringComparison.Ordinal);
                                if (index3 != -1)
                                {
                                    clothingSetName = asset.name.Substring(0, index3).Replace('_', ' ');
                                    continue;
                                }
                            }
                        }
                        else if (clothingSetName != null && asset.name.StartsWith(clothingSetName, StringComparison.Ordinal))
                        {
                            continue;
                        }
                    }
                }

                groups.Add(new SimplifiedItemListEntry(null, null, 1, redir.RedirectType));
            }
            else
            {
                ItemAsset? asset = item.GetItem(kit, faction, out _, out _);
                if (asset != null)
                {
                    if (asset.id > 30000 && asset is ItemClothingAsset)
                    {
                        if (clothingSetName == null && asset is ItemShirtAsset or ItemPantsAsset)
                        {
                            int index3 = asset.name.IndexOf(asset is ItemShirtAsset ? "_Top" : "_Bottom", StringComparison.Ordinal);
                            if (index3 != -1)
                            {
                                clothingSetName = asset.name.Substring(0, index3).Replace('_', ' ');
                                continue;
                            }
                        }
                        else if (clothingSetName != null && asset.name.StartsWith(clothingSetName, StringComparison.Ordinal))
                        {
                            continue;
                        }
                    }
                    int index2 = groups.FindLastIndex(x => x.Asset == asset);
                    if (index2 == -1)
                    {
                        groups.Add(new SimplifiedItemListEntry(asset, null, 1, RedirectType.None));
                    }
                    else
                    {
                        SimplifiedItemListEntry grp = groups[index2];
                        groups[index2] = new SimplifiedItemListEntry(grp.Asset, null, grp.Count + 1, RedirectType.None);
                    }
                }
            }
        }

        if (clothingSetName != null)
        {
            int ind = 0;
            for (int i = 0; i < groups.Count; ++i)
            {
                if (groups[i].Asset is ItemGunAsset)
                    ind = i + 1;
                else break;
            }

            groups.Insert(ind, new SimplifiedItemListEntry(null, clothingSetName, 1, RedirectType.None));
        }

        return groups;
    }
}
