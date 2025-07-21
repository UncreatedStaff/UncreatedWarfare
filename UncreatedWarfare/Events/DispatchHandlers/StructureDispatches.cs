using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="StructureManager.onDeployStructureRequested"/> when a player tries to place a structure. Can be cancelled.
    /// </summary>
    private void StructureManagerOnDeployStructureRequested(Structure structure, ItemStructureAsset asset, ref Vector3 point, ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(owner);
        if (player == null)
        {
            return;
        }

        if (!Regions.tryGetCoordinate(point, out byte x, out byte y))
        {
            shouldAllow = false;
            return;
        }

        PlaceStructureRequested args = new PlaceStructureRequested
        {
            Structure = structure,
            Position = point,
            Region = StructureManager.regions[x, y],
            RegionPosition = new RegionCoord(x, y),
            Rotation = Quaternion.Euler(angleX, angleY, angleZ),
            Owner = new CSteamID(owner),
            OriginalPlacer = player,
            GroupOwner = new CSteamID(group)
        };

        byte equippedX = args.OriginalPlacer.UnturnedPlayer.equipment.equipped_x;
        byte equippedY = args.OriginalPlacer.UnturnedPlayer.equipment.equipped_y;
        byte equippedPage = args.OriginalPlacer.UnturnedPlayer.equipment.equippedPage;
        byte equippedIndex = args.OriginalPlacer.UnturnedPlayer.inventory.getIndex(equippedPage, equippedX, equippedY);
        ItemJar oldEquippedJar = args.OriginalPlacer.UnturnedPlayer.inventory.getItem(equippedPage, equippedIndex);

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, continuation: args =>
        {
            if (!args.OriginalPlacer.IsOnline)
                return;

            // check if the item hasn't been moved in invetory for some reason
            ItemJar newEquippedJar = args.OriginalPlacer.UnturnedPlayer.inventory.getItem(equippedPage, equippedIndex);
            if (newEquippedJar == null || oldEquippedJar != newEquippedJar)
                return;

            StructureManager.dropReplicatedStructure(args.Structure, args.Position, args.Rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);

            // since shouldAllow is immediately set to false when the contiuation runs, the item doesn't get consumed in the player's hands.
            // so we need to manually remove it
            args.OriginalPlacer.UnturnedPlayer.inventory.removeItem(equippedPage, equippedIndex);

            // if there is another item of the same type, try to equip it
            InventorySearch inventorySearch = args.OriginalPlacer.UnturnedPlayer.inventory.has(newEquippedJar.item.id);
            if (inventorySearch == null)
                return;

            args.OriginalPlacer.UnturnedPlayer.inventory.ReceiveDragItem(inventorySearch.page, inventorySearch.jar.x, inventorySearch.jar.y, equippedPage, equippedX, equippedY, newEquippedJar.rot);
            args.OriginalPlacer.UnturnedPlayer.equipment.ServerEquip(equippedPage, equippedX, equippedY);
        });

        if (!shouldAllow)
            return;

        Vector3 rot = args.Rotation.eulerAngles;

        point = args.Position;
        angleX = rot.x;
        angleY = rot.y;
        angleZ = rot.z;
        owner = args.Owner.m_SteamID;
        group = args.GroupOwner.m_SteamID;
    }

    /// <summary>
    /// Invoked by <see cref="StructureManager.onStructureSpawned"/> after a player places a structure.
    /// </summary>
    private void StructureManagerOnStructureSpawned(StructureRegion region, StructureDrop drop)
    {
        StructureData data = drop.GetServersideData();

        WarfarePlayer? owner = new CSteamID(data.owner).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
            ? _playerService.GetOnlinePlayerOrNull(data.owner)
            : null;

        Regions.tryGetCoordinate(data.point, out byte x, out byte y);
        
        StructurePlaced args = new StructurePlaced
        {
            Structure = drop,
            ServersideData = data,
            Owner = owner,
            RegionPosition = new RegionCoord(x, y),
            Region = region
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }

    /// <summary>
    /// Invoked by <see cref="StructureDrop.OnSalvageRequested_Global"/> before a player salvages a structure.
    /// </summary>
    private void StructureDropOnSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        DestroyerComponent.AddOrUpdate(structure.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, true, EDamageOrigin.Unknown);

        WarfarePlayer player = _playerService.GetOnlinePlayer(instigatorClient);

        if (!StructureManager.tryGetRegion(structure.model, out byte x, out byte y, out StructureRegion region))
        {
            ILogger logger = GetLogger(typeof(StructureDrop), nameof(StructureDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify region of {0} structure salvaged by {1}.", structure.asset.itemName, instigatorClient.playerID.playerName);
            return;
        }

        SalvageStructureRequested args = new SalvageStructureRequested
        {
            Player = player,
            InstanceId = structure.instanceID,
            Region = region,
            Structure = structure,
            ServersideData = structure.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            InstigatorTeam = player.Team
        };

        BuildableExtensions.SetDestroyInfo(structure.model, args, null);
        
        try
        {
            bool shouldAllowTemp = shouldAllow;
            BuildableExtensions.SetSalvageInfo(structure.model, EDamageOrigin.Unknown, instigatorClient.playerID.steamID, true, salvageInfo =>
            {
                if (salvageInfo is not ISalvageListener listener)
                    return true;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllowTemp = false;

                return !args.IsCancelled;
            });

            shouldAllow = shouldAllowTemp;

            EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, continuation: args =>
            {
                if (args.ServersideData.structure.isDead)
                    return;

                // simulate StructureDrop.ReceiveSalvageRequest
                ItemStructureAsset? asset = args.Structure.asset;
                if (asset is { isUnpickupable: true })
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, EDamageOrigin.Unknown, args.Steam64, true, null);

                if (asset != null)
                {
                    // add salvaged item
                    if (args.ServersideData.structure.health >= asset.health)
                    {
                        args.Player.UnturnedPlayer.inventory.forceAddItem(new Item(asset, EItemOrigin.NATURE), true);
                    }
                    else if (asset.isSalvageable)
                    {
                        ItemAsset? salvagable = asset.FindSalvageItemAsset();
                        if (salvagable != null)
                        {
                            args.Player.UnturnedPlayer.inventory.forceAddItem(new Item(salvagable, EItemOrigin.NATURE), true);
                        }
                    }
                }

                if (!StructureManager.tryGetRegion(args.Structure.model, out byte x, out byte y, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                }

                StructureManager.destroyStructure(args.Structure, x, y, args.Player.IsOnline ? (args.Structure.model.position - args.Player.Position).normalized * 100f : new Vector3(0f, 100f, 0f), true);
            });
        }
        finally
        {
            // undo setting this if the task needs continuing, it'll be re-set later
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(structure.model, EDamageOrigin.Unknown, null, false, null);
            }
        }
    }

    private static bool _ignoreStructureManagerOnDamageStructureRequested;
    private void StructureManagerOnDamageStructureRequested(CSteamID instigatorSteamId, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (_ignoreStructureManagerOnDamageStructureRequested)
            return;

        StructureDrop? drop = StructureManager.FindStructureByRootTransform(structureTransform);
        if (drop == null)
        {
            shouldAllow = false;
            return;
        }

        StructureManager.tryGetRegion(structureTransform, out byte x, out byte y, out StructureRegion region);
        int index = region.drops.IndexOf(drop);

        if (index is < 0 or > ushort.MaxValue)
        {
            shouldAllow = false;
            _logger.LogWarning("Failed to find structure {0} # {1} in StructureManagerOnDamageStructureRequested.", drop.asset.FriendlyName, drop.instanceID);
            return;
        }

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(instigatorSteamId);

        DamageStructureRequested args = new DamageStructureRequested
        {
            InstigatorId = instigatorSteamId,
            Instigator = player,
            InstanceId = drop.instanceID,
            Structure = drop,
            DamageOrigin = damageOrigin,

            // todo
            PrimaryAsset = null,
            SecondaryAsset = null,

            RegionPosition = new RegionCoord(x, y),
            Region = region,
            ServersideData = drop.GetServersideData(),
            RegionIndex = (ushort)index,
            PendingDamage = pendingTotalDamage,
            Direction = StructureManagerSaveDirecction.LastDirection,
            InstigatorTeam = player?.Team ?? Team.NoTeam
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, continuation: args =>
        {
            if (args.Structure == null || args.Structure.GetServersideData().structure.isDead)
                return;

            _ignoreStructureManagerOnDamageStructureRequested = true;
            try
            {
                BuildableExtensions.SetDestroyInfo(args.Transform, args, null);
                BuildableExtensions.SetSalvageInfo(args.Transform, args.DamageOrigin, null, false, null);
                DestroyerComponent.AddOrUpdate(args.Transform.gameObject, args.InstigatorId.m_SteamID, false, args.DamageOrigin);
                StructureManager.damage(args.Transform, args.Direction, (ushort)Math.Clamp(args.PendingDamage, 0f, ushort.MaxValue), 1, false, args.InstigatorId, args.DamageOrigin);
            }
            finally
            {
                _ignoreStructureManagerOnDamageStructureRequested = false;
            }
        });

        if (!shouldAllow)
            return;
        
        pendingTotalDamage = (ushort)Math.Clamp(args.PendingDamage, 0f, ushort.MaxValue);
        BuildableExtensions.SetDestroyInfo(args.Transform, args, null);
        BuildableExtensions.SetSalvageInfo(args.Transform, args.DamageOrigin, null, false, null);
        DestroyerComponent.AddOrUpdate(args.Transform.gameObject, args.InstigatorId.m_SteamID, false, args.DamageOrigin);
    }
}