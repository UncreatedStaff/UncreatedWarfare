using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Kits.Items;

/// <summary>
/// Used to describe items of a kit in a list of items.
/// </summary>
/// <remarks>Combines sets of the same type of armor together.</remarks>
public struct ItemDescriptor
{
    public ItemAsset? Asset { get; private init; }

    public string Icon { get; private init; }

    public string ItemName { get; private init; }

    public ImmutableArray<ItemDescriptorAttachment> Attachments { get; private init; }

    public int Amount { get; private set; }

    public ItemDescriptorType Type { get; private init; }

    /// <summary>
    /// Extracts all the <see cref="ItemDescriptor"/> entries for a kit.
    /// </summary>
    internal static ImmutableArray<ItemDescriptor> Gather(
        Kit kit,
        Team team,
        IKitItem[] items,
        IKitItemResolver kitItemResolver,
        ItemIconProvider iconProvider,
        bool rich = true,
        bool tmpro = true)
    {
        GameThread.AssertCurrent();

        ItemDescriptor[] list = new ItemDescriptor[items.Length];
        int listIndex = 0;

        ItemDescriptorAttachment[] attachmentBuffer = new ItemDescriptorAttachment[5];
        ItemClothingAsset?[] clothing = new ItemClothingAsset[7];

        foreach (IKitItem item in items)
        {
            KitItemResolutionResult resolved = kitItemResolver.ResolveKitItem(item, kit, team);
            if (resolved.Asset == null)
                continue;

            if (item is IClothingItem clothingItem)
            {
                // clothing will be processed later
                ref ItemClothingAsset? cloth = ref clothing[(int)clothingItem.ClothingType];
                if (cloth != null || resolved.Asset is not ItemClothingAsset resolvedClothing)
                {
                    // should never happen
                    continue;
                }

                cloth = resolvedClothing;
            }
            else
            {
                ItemDescriptorType type = ItemDescriptorType.Other;
                ImmutableArray<ItemDescriptorAttachment> attachments;

                if (resolved.Asset is ItemGunAsset gun)
                {
                    if (item is IPageItem pageItem)
                    {
                        type = pageItem.Page switch
                        {
                            Page.Primary => ItemDescriptorType.PrimaryGun,
                            Page.Secondary => ItemDescriptorType.SecondaryGun,
                            _ => ItemDescriptorType.TertiaryGun
                        };
                    }
                    else
                    {
                        type = ItemDescriptorType.Utility;
                    }

                    ushort sight    = Unsafe.ReadUnaligned<ushort>(ref resolved.State[0]);
                    ushort tactical = Unsafe.ReadUnaligned<ushort>(ref resolved.State[2]);
                    ushort grip     = Unsafe.ReadUnaligned<ushort>(ref resolved.State[4]);
                    ushort barrel   = Unsafe.ReadUnaligned<ushort>(ref resolved.State[6]);
                    ushort magazine = Unsafe.ReadUnaligned<ushort>(ref resolved.State[8]);

                    if (sight == gun.sightID)
                        sight = 0;
                    if (tactical == gun.tacticalID)
                        tactical = 0;
                    if (grip == gun.gripID)
                        grip = 0;
                    if (barrel == gun.barrelID)
                        barrel = 0;

                    ItemAsset? sightAsset    = sight    == 0 ? null : Assets.find(EAssetType.ITEM, sight)    as ItemSightAsset;
                    ItemAsset? tacticalAsset = tactical == 0 ? null : Assets.find(EAssetType.ITEM, tactical) as ItemTacticalAsset;
                    ItemAsset? gripAsset     = grip     == 0 ? null : Assets.find(EAssetType.ITEM, grip)     as ItemGripAsset;
                    ItemAsset? barrelAsset   = barrel   == 0 ? null : Assets.find(EAssetType.ITEM, barrel)   as ItemBarrelAsset;
                    ItemAsset? magazineAsset = magazine == 0 ? null : Assets.find(EAssetType.ITEM, magazine) as ItemMagazineAsset;

                    int index = 0;
                    if (magazineAsset != null)
                    {
                        attachmentBuffer[index] = new ItemDescriptorAttachment
                        {
                            AttachmentType = AttachmentType.Magazine,
                            Icon = iconProvider.GetIconOrNull(magazineAsset.GUID, rich, tmpro),
                            ItemName = magazineAsset.itemName
                        };
                        ++index;
                    }
                    if (sightAsset != null)
                    {
                        attachmentBuffer[index] = new ItemDescriptorAttachment
                        {
                            AttachmentType = AttachmentType.Sight,
                            Icon = iconProvider.GetIconOrNull(sightAsset.GUID, rich, tmpro),
                            ItemName = sightAsset.itemName
                        };
                        ++index;
                    }
                    if (barrelAsset != null)
                    {
                        attachmentBuffer[index] = new ItemDescriptorAttachment
                        {
                            AttachmentType = AttachmentType.Barrel,
                            Icon = iconProvider.GetIconOrNull(barrelAsset.GUID, rich, tmpro),
                            ItemName = barrelAsset.itemName
                        };
                        ++index;
                    }
                    if (gripAsset != null)
                    {
                        attachmentBuffer[index] = new ItemDescriptorAttachment
                        {
                            AttachmentType = AttachmentType.Grip,
                            Icon = iconProvider.GetIconOrNull(gripAsset.GUID, rich, tmpro),
                            ItemName = gripAsset.itemName
                        };
                        ++index;
                    }
                    if (tacticalAsset != null)
                    {
                        attachmentBuffer[index] = new ItemDescriptorAttachment
                        {
                            AttachmentType = AttachmentType.Tactical,
                            Icon = iconProvider.GetIconOrNull(tacticalAsset.GUID, rich, tmpro),
                            ItemName = tacticalAsset.itemName
                        };
                        ++index;
                    }

                    attachments = ImmutableArray.Create(attachmentBuffer, 0, index);
                }
                else
                {
                    Guid guid = resolved.Asset.GUID;
                    bool found = false;
                    for (int j = 0; j < listIndex; ++j)
                    {
                        ref ItemDescriptor d = ref list[j];
                        if (d.Asset == null || d.Asset.GUID != guid)
                            continue;

                        ++d.Amount;
                        found = true;
                    }

                    if (found)
                        continue;

                    attachments = default;
                }

                string? icon;
                if (item is IRedirectedItem redir)
                {
                    icon = iconProvider.GetIconOrNull(redir.Item, rich, tmpro);
                }
                else
                {
                    icon = iconProvider.GetIconOrNull(resolved.Asset.GUID, rich, tmpro);
                }

                switch (resolved.Asset)
                {
                    case ItemThrowableAsset throwable:
                        if (icon == null)
                        {
                            if (throwable is { isExplosive: false, isFlash: false }
                                && throwable.name.Contains("smoke", StringComparison.OrdinalIgnoreCase))
                            {
                                icon = iconProvider.GetIcon(RedirectType.StandardSmokeGrenadeIcon, rich, tmpro);
                            }
                            else if (throwable.isExplosive)
                            {
                                icon = iconProvider.GetIcon(RedirectType.StandardGrenadeIcon, rich, tmpro);
                            }
                        }

                        type = ItemDescriptorType.Utility;
                        break;

                    case ItemMeleeAsset melee:
                        if (melee is { isLight: false, isRepair: false, isRepeated: false })
                        {
                            icon ??= iconProvider.GetIcon(RedirectType.StandardMeleeIcon, rich, tmpro);
                        }

                        type = ItemDescriptorType.Utility;
                        break;

                    case ItemMagazineAsset:
                        icon ??= iconProvider.GetIcon(RedirectType.StandardAmmoIcon, rich, tmpro);
                        type = ItemDescriptorType.Ammunition;
                        break;

                    default:
                        switch (resolved.Asset.type)
                        {
                            case EItemType.FOOD:
                            case EItemType.WATER:
                            case EItemType.MEDICAL:
                            case EItemType.MELEE:
                            case EItemType.FUEL:
                            case EItemType.TOOL:
                            case EItemType.BARRICADE:
                            case EItemType.STORAGE:
                            case EItemType.THROWABLE:
                            case EItemType.STRUCTURE:
                            case EItemType.TRAP:
                            case EItemType.OPTIC:
                            case EItemType.REFILL:
                            case EItemType.DETONATOR:
                            case EItemType.CHARGE:
                            case EItemType.FILTER:
                            case EItemType.VEHICLE_REPAIR_TOOL:
                            case EItemType.VEHICLE_LOCKPICK_TOOL:
                            case EItemType.TIRE:
                            // maybe nightvision
                            case EItemType.GLASSES:
                                type = ItemDescriptorType.Utility;
                                break;

                            case EItemType.SIGHT:
                            case EItemType.TACTICAL:
                            case EItemType.GRIP:
                            case EItemType.BARREL:
                            case EItemType.MAGAZINE:
                                type = ItemDescriptorType.Ammunition;
                                break;
                        }

                        break;
                }

                ItemDescriptor descriptor = new ItemDescriptor
                {
                    Amount = 1,
                    Attachments = attachments,
                    Icon = icon ?? Class.None.GetIconString(),
                    ItemName = resolved.Asset.itemName,
                    Type = type,
                    Asset = resolved.Asset
                };

                list[listIndex] = descriptor;
                ++listIndex;
            }
        }

        WorkingArmorSet[] sets = new WorkingArmorSet[7];
        int setNameCount = 0;

        for (int i = 0; i < 7; ++i)
        {
            ClothingType type = (ClothingType)i;
            ItemClothingAsset? asset = clothing[i];
            if (asset == null)
                continue;

            // glasses aren't really set-specific
            // backpacks get more specific and it may be important to know 
            //  whether a kit has a ruggedpack, dufflebag, etc.
            if (type is ClothingType.Glasses or ClothingType.Backpack)
                continue;

            // clothing names look like this:

            // Balaclava_Red    (non-set mask)
            // Cloudy_FacePaint (rn the only mask that goes with a set)
            // Cloudy_Helmet
            // Cloudy_Top
            // Cloudy_Bottom
            // Cloudy_Vest_L3 / Cloudy_TacticalRig

            int firstUnderscoreIndex = asset.name.IndexOf('_');
            int worth = type switch
            {
                ClothingType.Shirt or ClothingType.Pants => 3,
                ClothingType.Backpack or ClothingType.Vest => 2,
                _ => 1
            };
            int mask = 1 << i;

            if (firstUnderscoreIndex == -1 || firstUnderscoreIndex <= 0 || firstUnderscoreIndex >= asset.name.Length - 1)
                continue;

            ReadOnlySpan<char> setName = asset.name.AsSpan(0, firstUnderscoreIndex);
            bool found = false;
            for (int s = 0; s < setNameCount; ++s)
            {
                ref WorkingArmorSet set = ref sets[s];
                if (!set.Name.Equals(setName, StringComparison.OrdinalIgnoreCase))
                    continue;

                set.Worth += worth;
                set.Mask |= mask;
                ++set.Amount;
                found = true;
            }

            if (!found)
            {
                ref WorkingArmorSet set = ref sets[setNameCount];
                set.Name = setName.ToString();
                set.Worth = worth;
                set.Mask = mask;
                set.Amount = 1;
                ++setNameCount;
            }
        }

        int mostWorth = 0, mostWorthIndex = -1, mostAmount = 0, mostAmountIndex = -1;

        for (int i = 0; i < setNameCount; ++i)
        {
            ref WorkingArmorSet set = ref sets[i];
            if (set.Amount > mostAmount)
            {
                mostAmount = set.Amount;
                mostAmountIndex = i;
            }
            if (set.Worth > mostWorth)
            {
                mostWorth = set.Worth;
                mostWorthIndex = i;
            }
        }

        if (mostWorthIndex >= 0)
        {
            // prefer by worth, then by amount
            int index = -1;
            if (sets[mostWorthIndex].Amount > 1)
            {
                index = mostWorthIndex;
            }
            else if (sets[mostAmountIndex].Amount > 1)
            {
                index = mostAmountIndex;
            }

            if (index >= 0)
            {
                ref WorkingArmorSet set = ref sets[index];
                ClothingType iconType = (ClothingType)255;
                for (int i = 0; i < 7; ++i)
                {
                    if ((set.Mask & (1 << i)) != 0)
                        clothing[i] = null;
                    // dont show a shirt if the shirt isn't part of the set
                    else if (iconType == (ClothingType)255)
                        iconType = (ClothingType)i;
                }

                list[listIndex] = new ItemDescriptor
                {
                    Amount = 1,
                    Icon = iconProvider.GetIcon((RedirectType)iconType, rich, tmpro),
                    ItemName = set.Name + " Set",
                    Type = ItemDescriptorType.ClothingSet
                };
                ++listIndex;
            }
        }

        // add remaining clothes
        for (int i = 0; i < 7; ++i)
        {
            if (clothing[i] is not { } cloth)
                continue;

            string icon = iconProvider.GetIconOrNull(cloth.GUID, rich, tmpro)
                          ?? iconProvider.GetIcon((RedirectType)i, rich, tmpro);

            list[listIndex] = new ItemDescriptor
            {
                Amount = 1,
                Icon = icon,
                ItemName = cloth.itemName,
                Type = ItemDescriptorType.Clothing,
                Asset = cloth
            };
            ++listIndex;
        }

        // sort
        Array.Sort(list, 0, listIndex, DescriptorComparer);
        return ImmutableArray.Create(list, 0, listIndex);
    }

    private static readonly IComparer<ItemDescriptor> DescriptorComparer = Comparer<ItemDescriptor>.Create((a, b) => ((int)a.Type).CompareTo((int)b.Type));

    private struct WorkingArmorSet
    {
        public string Name;
        public int Worth;
        public int Mask;
        public int Amount;
    }
}

public readonly struct ItemDescriptorAttachment
{
    public AttachmentType AttachmentType { get; init; }

    public string ItemName { get; init; }

    public string? Icon { get; init; }
}

public enum ItemDescriptorType
{
    /// <summary>Primary weapon.</summary>
    PrimaryGun,

    /// <summary>Secondary weapon.</summary>
    SecondaryGun,

    /// <summary>Tertiary weapon.</summary>
    TertiaryGun,

    /// <summary>Spare magazines.</summary>
    Ammunition,

    /// <summary>Set of multiple of the same types of clothing.</summary>
    ClothingSet,

    /// <summary>Non-set clothing.</summary>
    Clothing,

    /// <summary>Utility items (melee, grenades, smokes, traps, etc.)</summary>
    Utility,

    /// <summary>Medical supplies.</summary>
    Medical,

    /// <summary>Other items.</summary>
    Other
}