using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Structures;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    /// <summary>
    /// Invoked by <see cref="StructureManager.onDeployStructureRequested"/> when a player tries to place a structure. Can be cancelled.
    /// </summary>
    private void StructureManagerOnDeployStructureRequested(Structure structure, ItemStructureAsset asset, ref Vector3 point, ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

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

            Vector3 rot = args.Rotation.eulerAngles;

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

            StructureManager.dropReplicatedStructure(args.Structure, args.Position, args.Rotation, args.Owner.m_SteamID, args.GroupOwner.m_SteamID);
        });
    }

    /// <summary>
    /// Invoked by <see cref="StructureManager.onStructureSpawned"/> after a player places a structure.
    /// </summary>
    private void StructureManagerOnStructureSpawned(StructureRegion region, StructureDrop drop)
    {
        StructureData data = drop.GetServersideData();

        UCPlayer? owner = new CSteamID(data.owner).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
            ? UCPlayer.FromID(data.owner)
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

        _ = DispatchEventAsync(args, CancellationToken.None);
    }

    /// <summary>
    /// Invoked by <see cref="StructureDrop.OnSalvageRequested_Global"/> before a player salvages a structure.
    /// </summary>
    private void StructureDropOnSalvageRequested(StructureDrop structure, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        DestroyerComponent.AddOrUpdate(structure.model.gameObject, instigatorClient.playerID.steamID.m_SteamID, EDamageOrigin.Unknown);

        UCPlayer? player = UCPlayer.FromSteamPlayer(instigatorClient);
        if (player == null)
        {
            ILogger logger = GetLogger(typeof(StructureDrop), nameof(StructureDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify player that salvaged a {0} structure.", structure.asset.itemName);
            return;
        }

        if (!StructureManager.tryGetRegion(structure.model, out byte x, out byte y, out StructureRegion region))
        {
            ILogger logger = GetLogger(typeof(StructureDrop), nameof(StructureDrop.OnSalvageRequested_Global));
            logger.LogError("Unable to identify region of {0} structure salvaged by {1}.", structure.asset.itemName, instigatorClient.playerID.playerName);
            return;
        }

        SalvageStructureRequested args = new SalvageStructureRequested(region)
        {
            Player = player,
            InstanceId = structure.instanceID,
            Structure = structure,
            ServersideData = structure.GetServersideData(),
            RegionPosition = new RegionCoord(x, y)
        };

        structure.model.GetComponents(WorkingDestroyInfo);
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
        structure.model.GetComponents(_workingSalvageInfos);
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

            if (args.ServersideData.structure.isDead)
                return;

            // simulate StructureDrop.ReceiveSalvageRequest
            ItemStructureAsset? asset = args.Structure.asset;
            if (asset is { isUnpickupable: true })
                return;

            // re-apply ISalvageInfo components
            structure.model.GetComponents(_workingSalvageInfos);
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

            if (asset != null)
            {
                // add salvaged item
                if (args.ServersideData.structure.health >= asset.health)
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
            }

            if (!StructureManager.tryGetRegion(structure.model, out byte x, out byte y, out _))
            {
                x = args.RegionPosition.x;
                y = args.RegionPosition.y;
            }

            StructureManager.destroyStructure(args.Structure, x, y, player.IsOnline ? (structure.model.position - player.Position).normalized * 100f : new Vector3(0f, 100f, 0f), true);
        });
    }
}