using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Teams;

/// <summary>
/// Handles swapping out items depending on which team is getting the kit.
/// </summary>
public class AssetRedirectService
{
    private readonly ITeamManager<Team> _teamManager;
    private readonly IFactionDataStore _factions;
    private readonly AssetConfiguration _assetConfig;
    private readonly FobConfiguration _fobConfiguration;

    public AssetRedirectService(ITeamManager<Team> teamManager, IFactionDataStore factions, AssetConfiguration assetConfig, FobConfiguration fobConfiguration)
    {
        _teamManager = teamManager;
        _factions = factions;
        _assetConfig = assetConfig;
        _fobConfiguration = fobConfiguration;
    }

    public bool TryFindRedirectType(ItemAsset item, out RedirectType type, out FactionInfo? faction, out string? variant, bool clothingOnly = false)
    {
        Guid itemGuid = item.GUID;

        // first check active teams then check all other factions
        foreach (Team team in _teamManager.AllTeams)
        {
            RedirectType t = CheckFactionSpecificRedirects(team.Faction, itemGuid, out variant, clothingOnly);
            if (t == RedirectType.None)
                continue;

            faction = team.Faction;
            type = t;
            return true;
        }

        foreach (FactionInfo existingFaction in _factions.Factions)
        {
            if (_teamManager.AllTeams.Any(x => x.Faction.Equals(existingFaction)))
                continue;

            RedirectType t = CheckFactionSpecificRedirects(existingFaction, itemGuid, out variant, clothingOnly);
            if (t == RedirectType.None)
                continue;

            faction = existingFaction;
            type = t;
            return true;
        }

        faction = null;
        variant = null;
        type = RedirectType.None;
        if (clothingOnly)
            return false;

        // todo add more
        if (item is ItemThrowableAsset && _assetConfig.GetAssetLink<ItemThrowableAsset>("Items:AmmoBag").MatchGuid(itemGuid))
            type = RedirectType.AmmoBag;

        else if (item is ItemPlaceableAsset && _assetConfig.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:InsurgencyCache").MatchGuid(itemGuid))
            type = RedirectType.Cache;

        else if (item is ItemPlaceableAsset && _assetConfig.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:VehicleSpawner").MatchGuid(itemGuid))
            type = RedirectType.VehicleBay;

        else if (item is ItemMeleeAsset && _assetConfig.GetAssetLink<ItemMeleeAsset>("Items:EntrenchingTool").MatchGuid(itemGuid))
            type = RedirectType.EntrenchingTool;

        else if (item is ItemGunAsset && _assetConfig.GetAssetLink<ItemGunAsset>("Items:LaserDesignator").MatchGuid(itemGuid))
            type = RedirectType.LaserDesignator;

        else if (item is ItemPlaceableAsset && _fobConfiguration.Shovelables.Any(x => x.ConstuctionType == ShovelableType.AmmoCrate && x.Foundation.MatchAsset(item)))
            type = RedirectType.AmmoCrate;

        else if (item is ItemPlaceableAsset && _fobConfiguration.Shovelables.Any(x => x.ConstuctionType == ShovelableType.AmmoCrate && x.CompletedStructure.MatchAsset(item)))
            type = RedirectType.AmmoCrateBuilt;

        else if (item is ItemPlaceableAsset && _fobConfiguration.Shovelables.Any(x => x.ConstuctionType == ShovelableType.RepairStation && x.Foundation.MatchAsset(item)))
            type = RedirectType.RepairStation;

        else if (item is ItemPlaceableAsset && _fobConfiguration.Shovelables.Any(x => x.ConstuctionType == ShovelableType.RepairStation && x.CompletedStructure.MatchAsset(item)))
            type = RedirectType.RepairStationBuilt;

        else
        {
            return false;
        }

        return true;

        RedirectType CheckFactionSpecificRedirects(FactionInfo existingFaction, Guid item, out string? variant, bool clothingOnly)
        {
            if (existingFaction.Shirts.TryMatchVariant(item, out variant))
                return RedirectType.Shirt;

            if (existingFaction.Pants.TryMatchVariant(item, out variant))
                return RedirectType.Pants;

            if (existingFaction.Vests.TryMatchVariant(item, out variant))
                return RedirectType.Vest;

            if (existingFaction.Hats.TryMatchVariant(item, out variant))
                return RedirectType.Hat;

            if (existingFaction.Masks.TryMatchVariant(item, out variant))
                return RedirectType.Mask;

            if (existingFaction.Backpacks.TryMatchVariant(item, out variant))
                return RedirectType.Backpack;

            if (existingFaction.Glasses.TryMatchVariant(item, out variant))
                return RedirectType.Glasses;

            if (!clothingOnly)
            {
                if (existingFaction.Build.MatchGuid(item))
                    return RedirectType.BuildSupply;

                if (existingFaction.Ammo.MatchGuid(item))
                    return RedirectType.AmmoSupply;

                if (existingFaction.RallyPoint.MatchGuid(item))
                    return RedirectType.RallyPoint;

                if (existingFaction.MapTackFlag.MatchGuid(item))
                    return RedirectType.MapTackFlag;

                if (existingFaction.FOBRadio.MatchGuid(item))
                    return RedirectType.Radio;
            }

            variant = null;
            return RedirectType.None;
        }
    }


    public ItemAsset? ResolveRedirect(RedirectType type, string variant, FactionInfo? kitFaction, Team requesterTeam, out byte[] state, out byte amount)
    {
        // expectation is that 'state' returns a copy of a new array (unless it's empty).

        state = Array.Empty<byte>();
        amount = 0;
        ItemAsset? toReturn = null;
        switch (type)
        {
            case RedirectType.Shirt:
                toReturn = kitFaction?.Shirts.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Shirts.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Pants:
                toReturn = kitFaction?.Pants.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Pants.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Vest:
                toReturn = kitFaction?.Vests.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Vests.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Backpack:
                toReturn = kitFaction?.Backpacks.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Backpacks.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Glasses:
                toReturn = kitFaction?.Glasses.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Glasses.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Mask:
                toReturn = kitFaction?.Masks.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Masks.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.Hat:
                toReturn = kitFaction?.Hats.Resolve(variant)?.GetAsset();
                if (!Equals(requesterTeam.Faction, kitFaction))
                    toReturn ??= requesterTeam.Faction.Hats.Resolve(variant)?.GetAsset();
                break;

            case RedirectType.BuildSupply:
                toReturn = requesterTeam.Faction.Build?.GetAsset();
                break;

            case RedirectType.AmmoSupply:
                toReturn = requesterTeam.Faction.Ammo?.GetAsset();
                break;

            case RedirectType.RallyPoint:
                toReturn = requesterTeam.Faction.RallyPoint?.GetAsset();
                break;

            case RedirectType.Radio:
                toReturn = requesterTeam.Faction.FOBRadio?.GetAsset();
                break;

            case RedirectType.MapTackFlag:
                toReturn = requesterTeam.Faction.MapTackFlag?.GetAsset();
                break;

            case RedirectType.AmmoBag:
                toReturn = _assetConfig.GetAssetLink<ItemThrowableAsset>("Items:AmmoBag").GetAsset();
                break;
                
            case RedirectType.Cache:
                toReturn = _assetConfig.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:InsurgencyCache").GetAsset();
                break;
                
            case RedirectType.VehicleBay:
                toReturn = _assetConfig.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:VehicleSpawner").GetAsset();
                break;
                
            case RedirectType.EntrenchingTool:
                toReturn = _assetConfig.GetAssetLink<ItemMeleeAsset>("Items:EntrenchingTool").GetAsset();
                break;
                
            case RedirectType.LaserDesignator:
                toReturn = _assetConfig.GetAssetLink<ItemGunAsset>("Items:LaserDesignator").GetAsset();
                break;

            case RedirectType.AmmoCrate:
                _fobConfiguration.Shovelables.FirstOrDefault(x => x.ConstuctionType == ShovelableType.AmmoCrate)?.Foundation.TryGetAsset(out toReturn);
                break;

            case RedirectType.AmmoCrateBuilt:
                _fobConfiguration.Shovelables.FirstOrDefault(x => x.ConstuctionType == ShovelableType.AmmoCrate)?.CompletedStructure.TryGetAsset(out toReturn);
                break;

            case RedirectType.RepairStation:
                _fobConfiguration.Shovelables.FirstOrDefault(x => x.ConstuctionType == ShovelableType.RepairStation)?.Foundation.TryGetAsset(out toReturn);
                break;

            case RedirectType.RepairStationBuilt:
                _fobConfiguration.Shovelables.FirstOrDefault(x => x.ConstuctionType == ShovelableType.RepairStation)?.CompletedStructure.TryGetAsset(out toReturn);
                break;

            
            // todo finish adding items
        }

        if (toReturn == null)
        {
            amount = 1;
            return null;
        }

        state = toReturn.getState(EItemOrigin.ADMIN);
        if (amount <= 0)
            amount = toReturn.amount;

        return toReturn;
    }
}