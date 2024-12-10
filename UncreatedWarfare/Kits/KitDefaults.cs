using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Kits;
public class KitDefaults(KitManager manager, IServiceProvider serviceProvider)
{
    private readonly LanguageService _languageService = serviceProvider.GetRequiredService<LanguageService>();
    private readonly ILogger<KitDefaults> _logger = serviceProvider.GetRequiredService<ILogger<KitDefaults>>();
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
    public static bool ShouldDequipOnExitVehicle(Class @class) => @class is Class.LAT or Class.HAT;
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
        using IServiceScope scope = serviceProvider.CreateScope();
        await using IKitsDbContext dbContext = scope.ServiceProvider.GetRequiredService<IKitsDbContext>();
        Kit? existing = await dbContext.Kits.FirstOrDefaultAsync(x => x.InternalName == name, token).ConfigureAwait(false);
        if (existing != null)
        {
            _logger.LogDebug("Found existing default kit: {0}.", existing.InternalName);
            return existing;
        }
        if (faction != null)
        {
            if (faction.Shirts.TryGetAsset("jacket", out ItemShirtAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Shirt }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Shirt, ClothingType.Shirt, null));
            if (faction.Pants.TryGetAsset("pants", out ItemPantsAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Pants }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Pants, ClothingType.Pants, null));
            if (faction.Vests.TryGetAsset("tact_rig", out ItemVestAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Vest }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Vest, ClothingType.Vest, null));
            if (faction.Hats.TryGetAsset("base", out ItemHatAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Hat }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Hat, ClothingType.Hat, null));
            if (faction.Masks.TryGetAsset(null, out ItemMaskAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Mask }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Mask, ClothingType.Mask, null));
            if (faction.Backpacks.TryGetAsset("ruggedpack", out ItemBackpackAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Backpack }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Backpack, ClothingType.Backpack, null));
            if (faction.Glasses.TryGetAsset(null, out ItemGlassesAsset? _) && !items.Exists(x => x is IClothingKitItem { Type: ClothingType.Glasses }))
                items.Add(new AssetRedirectClothingKitItem(0, RedirectType.Glasses, ClothingType.Glasses, null));
            
            existing = new Kit(name, Class.Unarmed, GetDefaultBranch(Class.Unarmed), KitType.Special, SquadLevel.Member, faction)
            {
                FactionFilterIsWhitelist = true
            };

            await dbContext.AddAsync(existing, token).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            existing.SetItemArray(items.ToArray(), dbContext);

            KitFilteredFaction kitFilteredFaction = new KitFilteredFaction { FactionId = faction.PrimaryKey, KitId = existing.PrimaryKey };
            existing.FactionFilter.Add(kitFilteredFaction);
            existing.FactionFilterIsWhitelist = true;

            existing.SetSignText(dbContext, CSteamID.Nil, "Unarmed Kit", _languageService.GetDefaultLanguage());

            dbContext.Update(existing);

            await dbContext.AddAsync(kitFilteredFaction, token).ConfigureAwait(false);
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

            await dbContext.AddAsync(existing, token).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            existing.SetSignText(dbContext, CSteamID.Nil, "Default Kit", _languageService.GetDefaultLanguage());

            existing.SetItemArray(items.ToArray(), dbContext);
            await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }

        ActionLog.Add(ActionLogType.CreateKit, name);
        Manager.Signs.UpdateSigns(existing);
        return existing;
    }

    public static IKitItem[] GetDefaultLoadoutItems(Class @class)
    {
        List<IKitItem> items = new List<IKitItem>(32)
        {
            // do not reorder these
            new AssetRedirectClothingKitItem(0u, RedirectType.Shirt, ClothingType.Shirt, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Pants, ClothingType.Pants, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Vest, ClothingType.Vest, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Hat, ClothingType.Hat, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Mask, ClothingType.Mask, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Backpack, ClothingType.Backpack, null),
            new AssetRedirectClothingKitItem(0u, RedirectType.Glasses, ClothingType.Glasses, null)
        };
        switch (@class)
        {
            case Class.Squadleader:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 0, 0, Page.Backpack, RedirectType.LaserDesignator, null));
                items.Add(new AssetRedirectPageKitItem(0u, 6, 1, 0, Page.Backpack, RedirectType.EntrenchingTool, null));
                items.Add(new AssetRedirectPageKitItem(0u, 0, 2, 0, Page.Backpack, RedirectType.Radio, null));
                items.Add(new AssetRedirectPageKitItem(0u, 3, 2, 0, Page.Backpack, RedirectType.Radio, null));
                items.Add(new AssetRedirectPageKitItem(0u, 0, 0, 1, Page.Shirt, RedirectType.RallyPoint, null));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Red Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Yellow Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("18713c6d9b8f4980bdee830ca9d667ef"), 4, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Rifleman:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));
                items.Add(new AssetRedirectPageKitItem(0u, 2, 0, 0, Page.Backpack, RedirectType.AmmoBag, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 1, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 4, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Medic:
                items[3] = new AssetRedirectClothingKitItem(0u, RedirectType.Hat, ClothingType.Hat, "medic");

                items.Add(new AssetRedirectPageKitItem(0u, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Bloodbags
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 0, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 1, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 2, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("5e1d521ecb7f4075aaebd344e838c2ca"), 3, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 5, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 4, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 5, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 6, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Breacher:
                items.Add(new AssetRedirectPageKitItem(0u, 3, 0, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // 12ga 00 Buckshot
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 0, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 0, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 1, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6089c30d75b247259673d9cdaa513cbb"), 1, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));

                // 12ga Rifled Slugs
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("d053c04af59b4985b463d160a92af331"), 2, 1, 0, Page.Backpack, 6, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("d053c04af59b4985b463d160a92af331"), 2, 0, 0, Page.Backpack, 6, Array.Empty<byte>()));

                // C-4 4-Pack Charge
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 0, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 2, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 3, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 4, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.AutomaticRifleman:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 3, 1, 1, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 1, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Grenadier:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 3, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.MachineGunner:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 1, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 1, 2, 1, Page.Hands, 1, Array.Empty<byte>()));
                break;
            case Class.LAT:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                break;
            case Class.HAT:
                items.Add(new AssetRedirectPageKitItem(0u, 4, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 5, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 3, 1, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Marksman:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 0, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.Sniper:
                // Backpack
                items.RemoveAt(5);
                items.Add(new AssetRedirectPageKitItem(0u, 1, 0, 0, Page.Vest, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Violet Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("1344161ee08e4297b64b4dc068c5935e"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 1, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Vest, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 0, 2, 0, Page.Vest, 1, Array.Empty<byte>()));

                // Laser Rangefinder
                if (Assets.find(new Guid("010de9d7d1fd49d897dc41249a22d436")) is ItemAsset rgf)
                    items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference(rgf.GUID), 3, 0, 0, Page.Vest, 1, rgf.getState(EItemOrigin.ADMIN)));
                break;
            case Class.APRifleman:
                items.Add(new AssetRedirectPageKitItem(0u, 0, 3, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Yellow Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("18713c6d9b8f4980bdee830ca9d667ef"), 4, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Remote-Detonated Claymore
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 3, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 3, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("6d5980d658c9449c941928bcc738f210"), 1, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 3, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 5, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 5, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 6, 1, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
            case Class.CombatEngineer:
                items.Add(new AssetRedirectPageKitItem(0u, 2, 2, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Anti-Tank Mine
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 2, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 4, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("92df865d6d534bc1b20b7885fddb8af3"), 6, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Frag Grenades
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 0, 3, 0, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 1, 3, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 0, 4, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Military Knife
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("47097f72d56c4bfb83bb8947e66396d5"), 2, 4, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Razorwire
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("a2a8a01a58454816a6c9a047df0558ad"), 6, 2, 1, Page.Backpack, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("a2a8a01a58454816a6c9a047df0558ad"), 7, 2, 1, Page.Backpack, 1, Array.Empty<byte>()));

                // Sandbag Lines
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("15f674dcaf3f44e19a124c8bf7e19ca2"), 0, 1, 0, Page.Shirt, 1, Array.Empty<byte>()));

                // Sandbag Pillboxes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("a9294335d8e84b76b1cbcb7d70f66aaa"), 3, 1, 0, Page.Shirt, 1, Array.Empty<byte>()));
                break;
            case Class.Crewman:
                items.RemoveAt(3); // hat
                items.RemoveRange(5 - 1, 2); // backpack, glasses

                // Crewman Helmet
                items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference("3ee3c7292ce340489b9afacda209e138"), ClothingType.Hat, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 4, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 2, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Portable Gas Can
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Page.Vest, 1, Array.Empty<byte>()));

                // Carjack
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Page.Vest, 1, Array.Empty<byte>()));
                break;
            case Class.Pilot:
                items.RemoveRange(2, 2); // vest, hat
                items.RemoveRange(5 - 2, 2); // backpack, glasses

                // Pilot Helmet
                items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference("78656047d47a4ff1ad7aa8a2e4d070a0"), ClothingType.Hat, Array.Empty<byte>()));

                // Red Smoke
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("c9fadfc1008e477ebb9aeaaf0ad9afb9"), 2, 0, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 3, 0, 0, Page.Hands, 1, Array.Empty<byte>()));
                
                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 1, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 1, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Portable Gas Can
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("d5b9f19e2f2a4ee2ab4dc666f32f7df3"), 0, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));

                // Carjack
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("1f80a9e0c86047d38b72e08e267885f6"), 2, 0, 0, Page.Shirt, 1, Array.Empty<byte>()));
                break;
            case Class.SpecOps:
                items.RemoveAt(6); // glasses
                items.Add(new AssetRedirectPageKitItem(0u, 4, 0, 1, Page.Backpack, RedirectType.EntrenchingTool, null));

                // Military Nightvision
                items.Add(new SpecificClothingKitItem(0u, new UnturnedAssetReference("cca8301927e049149fcee2b157a59da1"), ClothingType.Glasses, new byte[1]));

                // Dressings
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 0, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("ae46254cfa3b437e9d74a5963e161da4"), 1, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // Frag Grenade
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("b01e414db03747509e87ebc515744216"), 2, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // White Smokes
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 3, 2, 0, Page.Hands, 1, Array.Empty<byte>()));
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("7bf622df8cfe4d8c8b740fae3e95b957"), 4, 2, 0, Page.Hands, 1, Array.Empty<byte>()));

                // MRE
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("acf7e825832f4499bb3b7cbec4f634ca"), 0, 0, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Detonator
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("618d0402c0724f1582fffd69f4cc0868"), 0, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // C-4 4-Pack Charge
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("85bcbd5ee63d49c19c3c86b4e0d115d6"), 1, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));

                // Binoculars
                items.Add(new SpecificPageKitItem(0u, new UnturnedAssetReference("f260c581cf504098956f424d62345982"), 5, 2, 0, Page.Backpack, 1, Array.Empty<byte>()));
                break;
        }
        return items.ToArray();
    }
}
