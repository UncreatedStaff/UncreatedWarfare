using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked by <see cref="BarricadeManager.onDeployBarricadeRequested"/> when a player tries to place a barricade. Can be cancelled.
    /// </summary>
    private void BarricadeManagerOnDeployBarricadeRequested(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        InteractableVehicle? vehicle = null;
        BarricadeRegion? region = null;
        RegionCoord coords = default;
        ushort plant = ushort.MaxValue;
        if (hit != null && hit.CompareTag("Vehicle"))
        {
            int ct = Math.Min(ushort.MaxValue - 1, BarricadeManager.vehicleRegions.Count);
            for (int i = 0; i < ct; ++i)
            {
                VehicleBarricadeRegion vRegion = BarricadeManager.vehicleRegions[i];
                if (vRegion.parent != hit)
                    continue;

                region = vRegion;
                vehicle = vRegion.vehicle;
                plant = (ushort)i;
                break;
            }

            if (plant == ushort.MaxValue)
            {
                shouldAllow = false;
                return;
            }
        }

        if (region == null)
        {
            if (!Regions.tryGetCoordinate(point, out byte x, out byte y))
            {
                shouldAllow = false;
                return;
            }

            region = BarricadeManager.regions[x, y];
            coords.x = x;
            coords.y = y;
        }
        
        WarfarePlayer? onlinePlayer = _playerService.GetOnlinePlayerOrNull(owner);
        if (onlinePlayer == null)
            return; // this event does not support care packages being placed

        PlaceBarricadeRequested args = new PlaceBarricadeRequested
        {
            Barricade = barricade,
            TargetVehicle = vehicle,
            HitTarget = hit,
            Position = point,
            Region = region,
            VehicleRegionIndex = plant,
            RegionPosition = coords,
            Rotation = Quaternion.Euler(angleX, angleY, angleZ),
            Owner = new CSteamID(owner),
            OriginalPlacer = onlinePlayer,
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

            bool plantTargetAlive = args.TargetVehicle != null;
            if (!plantTargetAlive && args.TargetVehicle is not null || plantTargetAlive && args.HitTarget == null)
            {
                // vehicle or the component that was hit was destroyed since started
                return;
            }

            Vector3 rot = args.Rotation.eulerAngles;
            Quaternion rotation = BarricadeManager.getRotation(args.Barricade.asset, rot.x, rot.y, rot.z);
            
            if (plantTargetAlive)
            {
                BarricadeManager.dropPlantedBarricade(args.HitTarget, args.Barricade, args.Position, rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);
            }
            else
            {
                BarricadeManager.dropNonPlantedBarricade(args.Barricade, args.Position, rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);
            }
            
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
    /// Invoked by <see cref="BarricadeManager.onBarricadeSpawned"/> after a player places a barricade.
    /// </summary>
    private void BarricadeManagerOnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        BarricadeData data = drop.GetServersideData();

        IBuildable buildable = new BuildableBarricade(drop);
        BuildableContainer buildableContainer = drop.model.GetOrAddComponent<BuildableContainer>();
        buildableContainer.Init(buildable);

        WarfarePlayer? owner = new CSteamID(data.owner).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
            ? _playerService.GetOnlinePlayerOrNull(data.owner)
            : null;

        Regions.tryGetCoordinate(data.point, out byte x, out byte y);

        ushort plant = ushort.MaxValue;
        if (region is VehicleBarricadeRegion vRegion)
        {
            int ct = Math.Min(ushort.MaxValue - 1, BarricadeManager.vehicleRegions.Count);
            for (int i = 0; i < ct; ++i)
            {
                if (!ReferenceEquals(vRegion, BarricadeManager.vehicleRegions[i]))
                    continue;

                plant = (ushort)i;
                break;
            }
        }

        BarricadePlaced args = new BarricadePlaced
        {
            Barricade = drop,
            ServersideData = data,
            Owner = owner,
            VehicleRegionIndex = plant,
            RegionPosition = new RegionCoord(x, y),
            Region = region,
            Buildable = buildable
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, _unloadToken);
    }

    /// <summary>
    /// Invoked by <see cref="BarricadeDrop.OnSalvageRequested_Global"/> before a player salvages a barricade.
    /// </summary>
    private void BarricadeDropOnSalvageRequested(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        DestroyerComponent.AddOrUpdate(barricade.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, true, EDamageOrigin.Unknown);

        WarfarePlayer player = _playerService.GetOnlinePlayer(instigatorClient);

        if (!BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            ILogger logger = GetLogger(typeof(BarricadeDrop), nameof(BarricadeDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify region of {0} barricade salvaged by {1}.", barricade.asset.itemName, instigatorClient.playerID.playerName);
            return;
        }

        SalvageBarricadeRequested args = new SalvageBarricadeRequested
        {
            Player = player,
            Region = region,
            InstanceId = barricade.instanceID,
            Barricade = barricade,
            ServersideData = barricade.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            InstigatorTeam = player.Team
        };

        BuildableExtensions.SetDestroyInfo(barricade.model, args, null);
        
        try
        {
            bool shouldAllowTemp = shouldAllow;
            BuildableExtensions.SetSalvageInfo(barricade.model, EDamageOrigin.Unknown, instigatorClient.playerID.steamID, true, salvageInfo =>
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
                if (args.ServersideData.barricade.isDead)
                    return;

                // simulate BarricadeDrop.ReceiveSalvageRequest
                ItemBarricadeAsset asset = args.Barricade.asset;
                if (asset.isUnpickupable)
                    return;

                // re-apply ISalvageInfo components
                BuildableExtensions.SetSalvageInfo(args.Transform, EDamageOrigin.Unknown, args.Steam64, true, null);

                // add salvaged item
                if (args.ServersideData.barricade.health >= asset.health)
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

                if (!BarricadeManager.tryGetRegion(args.Barricade.model, out byte x, out byte y, out ushort plant, out _))
                {
                    x = args.RegionPosition.x;
                    y = args.RegionPosition.y;
                    plant = args.VehicleRegionIndex;
                }

                BarricadeManager.destroyBarricade(args.Barricade, x, y, plant);
            });
        }
        finally
        {
            // undo setting this if the task needs continuing, it'll be re-set later
            if (!shouldAllow)
            {
                BuildableExtensions.SetSalvageInfo(barricade.model, EDamageOrigin.Unknown, null, false, null);
            }
        }
    }

    /// <summary>
    /// Invoked by <see cref="BarricadeManager.onModifySignRequested"/> before a player edits the text on a sign.
    /// </summary>
    private void BarricadeManagerOnModifySignRequested(CSteamID instigator, InteractableSign sign, ref string text, ref bool shouldallow)
    {
        if (!shouldallow)
            return;

        if (!BarricadeManager.tryGetRegion(sign.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            shouldallow = false;
            return;
        }

        BarricadeDrop? drop = region.FindBarricadeByRootTransform(sign.transform);
        if (drop == null)
        {
            shouldallow = false;
            return;
        }

        text = sign.trimText(text);

        if (!sign.isTextValid(text))
        {
            _logger.LogWarning("Invalid text: {0} in sign.", text);
            shouldallow = false;
            return;
        }


        BarricadeData data = drop.GetServersideData();

        WarfarePlayer instigatorPlayer = _playerService.GetOnlinePlayer(instigator);

        ChangeSignTextRequested args = new ChangeSignTextRequested
        {
            Barricade = drop,
            Player = instigatorPlayer,
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            Region = region,
            Sign = sign,
            ServersideData = data,
            Text = text
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldallow, continuation: args =>
        {
            if (args.Sign == null || args.Barricade.GetServersideData().barricade.isDead)
                return;

            if (args.Player is not null)
            {
                BuildableContainer bcomp = BuildableContainer.Get(args.Barricade);
                bcomp.SignEditor = args.Player.Steam64;
            }

            BarricadeManager.ServerSetSignText(args.Sign, args.Text);
        });

        if (!shouldallow)
            return;

        if (instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            BuildableContainer bcomp = BuildableContainer.Get(args.Barricade);
            bcomp.SignEditor = instigator;
        }

        text = args.Text;
    }

    private static bool _ignoreBarricadeManagerOnDamageBarricadeRequested;
    private void BarricadeManagerOnDamageBarricadeRequested(CSteamID instigatorSteamId, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
    {
        if (_ignoreBarricadeManagerOnDamageBarricadeRequested)
            return;

        BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (drop == null)
        {
            shouldAllow = false;
            return;
        }

        BarricadeManager.tryGetRegion(barricadeTransform, out byte x, out byte y, out ushort plant, out BarricadeRegion region);
        int index = region.drops.IndexOf(drop);

        if (index is < 0 or > ushort.MaxValue)
        {
            shouldAllow = false;
            _logger.LogWarning("Failed to find barricade {0} # {1} in BarricadeManagerOnDamageBarricadeRequested.", drop.asset.FriendlyName, drop.instanceID);
            return;
        }

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(instigatorSteamId);

        DamageBarricadeRequested args = new DamageBarricadeRequested
        {
            InstigatorId = instigatorSteamId,
            Instigator = player,
            InstanceId = drop.instanceID,
            Barricade = drop,
            Region = region,
            DamageOrigin = damageOrigin,

            // todo
            PrimaryAsset = null,
            SecondaryAsset = null,

            RegionPosition = new RegionCoord(x, y),
            ServersideData = drop.GetServersideData(),
            VehicleRegionIndex = plant,
            RegionIndex = (ushort)index,
            PendingDamage = pendingTotalDamage,
            InstigatorTeam = player?.Team ?? Team.NoTeam
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldAllow, continuation: args =>
        {
            if (args.Barricade == null || args.Barricade.GetServersideData().barricade.isDead)
                return;

            _ignoreBarricadeManagerOnDamageBarricadeRequested = true;
            try
            {
                BuildableExtensions.SetDestroyInfo(args.Transform, args, null);
                BuildableExtensions.SetSalvageInfo(args.Transform, args.DamageOrigin, null, false, null);
                DestroyerComponent.AddOrUpdate(args.Transform.gameObject, args.InstigatorId.m_SteamID, false, args.DamageOrigin);
                BarricadeManager.damage(args.Transform, (ushort)Math.Clamp(args.PendingDamage, 0f, ushort.MaxValue), 1, false, args.InstigatorId, args.DamageOrigin);
            }
            finally
            {
                _ignoreBarricadeManagerOnDamageBarricadeRequested = false;
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