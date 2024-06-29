using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Components;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    private readonly List<ISalvageInfo> _workingSalvageInfos = new List<ISalvageInfo>(2);
    internal readonly List<IDestroyInfo> WorkingDestroyInfo = new List<IDestroyInfo>(2);

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

        PlaceBarricadeRequested args = new PlaceBarricadeRequested
        {
            Barricade = barricade,
            TargetVehicle = vehicle,
            HitTarget = hit,
            Position = point,
            Region = region,
            VehicleRegionIndex = plant,
            RegionPosition = coords,
            Rotation = new Vector3(angleX, angleY, angleZ),
            Owner = new CSteamID(owner),
            OriginalPlacer = UCPlayer.FromID(owner),
            GroupOwner = new CSteamID(group)
        };
        
        UniTask<bool> task = DispatchEventAsync(args, _unloadToken);

        if (task.Status != UniTaskStatus.Pending)
        {
            if (args.IsActionCancelled)
            {
                shouldAllow = false;
                return;
            }

            Vector3 rot = args.Rotation;

            point = args.Position;
            angleX = rot.x;
            angleY = rot.y;
            angleZ = rot.z;
            owner = args.Owner.m_SteamID;
            group = args.GroupOwner.m_SteamID;
            return;
        }

        // prevent placement then replace it once the task finishes
        shouldAllow = false;
        UniTask.Create(async () =>
        {
            if (!await task)
                return;
            
            await UniTask.SwitchToMainThread(_unloadToken);

            bool plantTargetAlive = args.TargetVehicle != null;
            if (!plantTargetAlive && args.TargetVehicle is not null || plantTargetAlive && args.HitTarget == null)
            {
                // vehicle or the component that was hit was destroyed since started
                return;
            }

            Vector3 rot = args.Rotation;
            Quaternion rotation = BarricadeManager.getRotation(args.Barricade.asset, rot.x, rot.y, rot.z);
            
            if (plantTargetAlive)
            {
                BarricadeManager.dropPlantedBarricade(args.HitTarget, args.Barricade, args.Position, rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);
            }
            else
            {
                BarricadeManager.dropNonPlantedBarricade(args.Barricade, args.Position, rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);
            }
        });
    }

    /// <summary>
    /// Invoked by <see cref="BarricadeManager.onBarricadeSpawned"/> after a player places a barricade.
    /// </summary>
    private void BarricadeManagerOnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        BarricadeData data = drop.GetServersideData();

        UCPlayer? owner = new CSteamID(data.owner).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
            ? UCPlayer.FromID(data.owner)
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
            Region = region
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }

    /// <summary>
    /// Invoked by <see cref="BarricadeDrop.OnSalvageRequested_Global"/> before a player salvages a barricade.
    /// </summary>
    private void BarricadeDropOnSalvageRequested(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        DestroyerComponent.AddOrUpdate(barricade.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, EDamageOrigin.Unknown);

        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null)
        {
            ILogger logger = GetLogger(typeof(BarricadeDrop), nameof(BarricadeDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify player that salvaged a {0} barricade.", barricade.asset.itemName);
            return;
        }

        if (!BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            ILogger logger = GetLogger(typeof(BarricadeDrop), nameof(BarricadeDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify region of {0} barricade salvaged by {1}.", barricade.asset.itemName, instigatorClient.playerID.playerName);
            return;
        }

        SalvageBarricadeRequested args = new SalvageBarricadeRequested(region)
        {
            Player = player,
            InstanceId = barricade.instanceID,
            Barricade = barricade,
            ServersideData = barricade.GetServersideData(),
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant
        };

        barricade.model.GetComponents(WorkingDestroyInfo);
        try
        {
            foreach (IDestroyInfo destroyInfo in WorkingDestroyInfo)
            {
                destroyInfo.DestroyInfo = args;
            }
        }
        finally
        {
            WorkingDestroyInfo.Clear();
        }

        UniTask<bool> task;

        // handle ISalvageInfo components
        barricade.model.GetComponents(_workingSalvageInfos);
        try
        {
            foreach (ISalvageInfo salvageInfo in _workingSalvageInfos)
            {
                salvageInfo.Salvager = instigatorClient.playerID.steamID;
                salvageInfo.IsSalvaged = true;
                if (salvageInfo is not ISalvageListener listener)
                    continue;

                listener.OnSalvageRequested(args);

                if (args.IsActionCancelled)
                    shouldAllow = false;

                if (args.IsCancelled)
                    return;
            }

            task = DispatchEventAsync(args, _unloadToken);

            if (task.Status != UniTaskStatus.Pending)
            {
                if (args.IsActionCancelled)
                    shouldAllow = false;

                return;
            }

            shouldAllow = false;
        }
        finally
        {
            try
            {
                // undo setting this if the task needs continuing, it'll be re-set later
                if (!shouldAllow)
                {
                    foreach (ISalvageInfo salvageInfo in _workingSalvageInfos)
                        salvageInfo.IsSalvaged = false;
                }
            }
            finally
            {
                _workingSalvageInfos.Clear();
            }
        }

        UniTask.Create(async () =>
        {
            if (!await task)
            {
                return;
            }

            await UniTask.SwitchToMainThread(_unloadToken);

            if (args.ServersideData.barricade.isDead)
                return;

            // simulate BarricadeDrop.ReceiveSalvageRequest
            ItemBarricadeAsset asset = args.Barricade.asset;
            if (asset.isUnpickupable)
                return;

            // re-apply ISalvageInfo components
            barricade.model.GetComponents(_workingSalvageInfos);
            try
            {
                for (int i = 0; i < _workingSalvageInfos.Count; ++i)
                {
                    ISalvageInfo salvageInfo = _workingSalvageInfos[i];
                    salvageInfo.Salvager = instigatorClient.playerID.steamID;
                    salvageInfo.IsSalvaged = true;
                }
            }
            finally
            {
                _workingSalvageInfos.Clear();
            }

            // add salvaged item
            if (args.ServersideData.barricade.health >= asset.health)
            {
                args.Player.Player.inventory.forceAddItem(new Item(asset, EItemOrigin.NATURE), true);
            }
            else if (asset.isSalvageable)
            {
                ItemAsset? salvagable = asset.FindSalvageItemAsset();
                if (salvagable != null)
                {
                    player.Player.inventory.forceAddItem(new Item(salvagable, EItemOrigin.NATURE), true);
                }
            }

            if (!BarricadeManager.tryGetRegion(barricade.model, out byte x, out byte y, out ushort plant, out _))
            {
                x = args.RegionPosition.x;
                y = args.RegionPosition.y;
                plant = args.VehicleRegionIndex;
            }

            BarricadeManager.destroyBarricade(args.Barricade, x, y, plant);
        });
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

        BarricadeData data = drop.GetServersideData();

        UCPlayer? instigatorPlayer = UCPlayer.FromCSteamID(instigator);
        if (instigatorPlayer == null)
        {
            shouldallow = false;
            return;
        }

        ChangeSignTextRequested args = new ChangeSignTextRequested
        {
            Barricade = drop,
            Player = instigatorPlayer,
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            Region = region,
            Sign = sign,
            ServersideData = data,
            Text = sign.text
        };

        UniTask<bool> task = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
        if (task.Status != UniTaskStatus.Pending)
        {
            if (args.IsActionCancelled)
            {
                shouldallow = false;
                return;
            }

            if (instigator.GetEAccountType() == EAccountType.k_EAccountTypeIndividual && sign.transform.TryGetComponent(out BarricadeComponent bcomp))
            {
                bcomp.LastEditor = instigator;
                bcomp.EditTick = UCWarfare.I.Debugger.Updates;
            }

            text = args.Text;
            return;
        }

        shouldallow = false;
        UniTask.Create(async () =>
        {
            if (!await task)
            {
                return;
            }

            await UniTask.SwitchToMainThread(_unloadToken);

            if (args.Player is not null && sign.transform.TryGetComponent(out BarricadeComponent bcomp))
            {
                bcomp.LastEditor = args.Player.CSteamID;
                bcomp.EditTick = UCWarfare.I.Debugger.Updates;
            }

            BarricadeManager.ServerSetSignText(args.Sign, args.Text);
        });
    }
}