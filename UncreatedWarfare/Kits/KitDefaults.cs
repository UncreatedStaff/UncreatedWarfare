using Microsoft.EntityFrameworkCore;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits;
public class KitDefaults<TDbContext>(KitManager manager) where TDbContext : IKitsDbContext, new()
{
    public KitManager Manager { get; } = manager;

    /// <returns>The number of ammo boxes required to refill the kit based on it's <see cref="Class"/>.</returns>
    public static int GetAmmoCost(Class @class) => @class switch
    {
        Class.HAT or Class.MachineGunner or Class.CombatEngineer => 3,
        Class.LAT or Class.AutomaticRifleman or Class.Grenadier => 2,
        _ => 1
    };
    public static float GetDefaultTeamLimit(Class @class) => @class switch
    {
        Class.HAT => 0.1f,
        _ => 1f
    };
    public static float GetDefaultRequestCooldown(Class @class) => @class switch
    {
        _ => 0f
    };
    public static Branch GetDefaultBranch(Class @class)
        => @class switch
        {
            Class.Pilot => Branch.Airforce,
            Class.Crewman => Branch.Armor,
            _ => Branch.Infantry
        };

    private static readonly IKitItem[] DefaultKitItems =
    {
        // MRE
        new SpecificPageKitItem(0, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Hands, 1, Array.Empty<byte>()),
        new SpecificPageKitItem(0, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 1, 0, 0, Page.Hands, 1, Array.Empty<byte>()),

        // Bottled Soda
        new SpecificPageKitItem(0, new UnturnedAssetReference("c83390665c6546b8befbf6f15ef202c4"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()),

        // Bottled Water
        new SpecificPageKitItem(0, new UnturnedAssetReference("f81d68ebb2a8490dbe1545d432b9c099"), 2, 1, 0, Page.Hands, 1, Array.Empty<byte>()),

        // Binoculars
        new SpecificPageKitItem(0, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()),

        // Earpiece
        new SpecificClothingKitItem(0, new UnturnedAssetReference("2ecf1b15a59f4125a2d55c88479529c2"), ClothingType.Mask, Array.Empty<byte>())
    };

    private static readonly IKitItem[] DefaultKitClothes =
    {
        new SpecificClothingKitItem(0, new UnturnedAssetReference("c3adf16156004b40a839ed1b80583c32"), ClothingType.Shirt, Array.Empty<byte>()),
        new SpecificClothingKitItem(0, new UnturnedAssetReference("67a6ec52e4b24ffd89f75ceee0eb5179"), ClothingType.Pants, Array.Empty<byte>())
    };

    public async Task<Kit> CreateDefaultKit(FactionInfo? faction, string name, CancellationToken token = default)
    {
        List<IKitItem> items = new List<IKitItem>(DefaultKitItems.Length + 6);
        items.AddRange(DefaultKitItems);
        await using IKitsDbContext dbContext = new TDbContext();
        Kit? existing = await dbContext.Kits.FirstOrDefaultAsync(x => x.InternalName == name, token).ConfigureAwait(false);
        if (existing != null)
            return existing;
        if (faction != null)
        {
            if (faction.DefaultShirt.ValidReference(out ItemShirtAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Shirt }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Shirt, ClothingType.Shirt, null));
            if (faction.DefaultPants.ValidReference(out ItemPantsAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Pants }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Pants, ClothingType.Pants, null));
            if (faction.DefaultVest.ValidReference(out ItemVestAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Vest }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Vest, ClothingType.Vest, null));
            if (faction.DefaultHat.ValidReference(out ItemHatAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Hat }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Hat, ClothingType.Hat, null));
            if (faction.DefaultMask.ValidReference(out ItemMaskAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Mask }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Mask, ClothingType.Mask, null));
            if (faction.DefaultBackpack.ValidReference(out ItemBackpackAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Backpack }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Backpack, ClothingType.Backpack, null));
            if (faction.DefaultGlasses.ValidReference(out ItemGlassesAsset _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Glasses }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Glasses, ClothingType.Glasses, null));
            
            existing = new Kit(name, Class.Unarmed, GetDefaultBranch(Class.Unarmed), KitType.Special, SquadLevel.Member, faction)
            {
                FactionFilterIsWhitelist = true
            };

            dbContext.Add(existing);
            existing.SetItemArray(items.ToArray(), dbContext);
            existing.FactionFilter.Add(new KitFilteredFaction { FactionId = faction.PrimaryKey, Kit = existing });
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        else
        {
            for (int i = 0; i < DefaultKitClothes.Length; ++i)
            {
                if (DefaultKitClothes[i] is IClothingKitItem jar && !items.Exists(x => x is IClothingKitItem jar2 && jar2.Type == jar.Type))
                {
                    items.Add(DefaultKitClothes[i]);
                }
            }

            existing = new Kit(name, Class.Unarmed, GetDefaultBranch(Class.Unarmed), KitType.Special, SquadLevel.Member, null);

            dbContext.Add(existing);
            existing.SetItemArray(items.ToArray(), dbContext);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        ActionLog.Add(ActionLogType.CreateKit, name);
        Manager.Signs.UpdateSigns(existing);
        return existing;
    }
}
