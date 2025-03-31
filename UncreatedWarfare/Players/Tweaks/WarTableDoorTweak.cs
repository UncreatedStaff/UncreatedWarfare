using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Tweaks;

public class WarTableDoorTweak :
    IEventListener<PlayerKitChanged>,
    IEventListener<PlayerTeamChanged>,
    IEventListener<QuestObjectInteracted>,
    IEventListener<PlayerJoined>,
    IEventListener<PlayerEnteredZone>,
    IEventListener<PlayerDeployed>
{
    private readonly ZoneStore _zoneStore;
    private readonly DeploymentService _deploymentService;

    private readonly IAssetLink<ObjectAsset>[] _teleportDoors;
    private readonly ushort _flagId;


    private static class Flags
    {
        public const short AlreadyDeployed = -3;
        public const short NoKit = -2;
        public const short Deploying = -1;
        public const short Open = 0;
    }

    public WarTableDoorTweak(AssetConfiguration assetConfig, ZoneStore zoneStore, DeploymentService deploymentService)
    {
        _zoneStore = zoneStore;
        _deploymentService = deploymentService;
        _teleportDoors = assetConfig.GetSection("Objects:TeleportToMainDoors").Get<IAssetLink<ObjectAsset>[]>()!;
        _flagId = assetConfig.GetValue<ushort>("Objects:TeleportToMainDoorFlag");
    }

    void IEventListener<QuestObjectInteracted>.HandleEvent(QuestObjectInteracted e, IServiceProvider serviceProvider)
    {
        if (!_teleportDoors.ContainsAsset(e.Object.asset))
            return;

        if (!_zoneStore.IsInsideZone(e.Player.Position, ZoneType.WarRoom, e.Player.Team.Faction))
        {
            UpdateFlag(e.Player);
            return;
        }

        Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, e.Player.Team.Faction);
        if (zone == null)
        {
            UpdateFlag(e.Player);
            return;
        }

        if (e.Player.Component<DeploymentComponent>().CurrentDeployment is Zone { Type: ZoneType.WarRoom })
        {
            UpdateFlag(e.Player);
            return;
        }

        if (_deploymentService.TryStartDeployment(e.Player, zone, new DeploySettings
        {
            Delay = TimeSpan.FromSeconds(1),
            DisableInitialChatUpdates = true,
            DisableTickingChatUpdates = true
        }))
        {
            e.Player.SetFlag(_flagId, Flags.Deploying);
        }
    }

    void IEventListener<PlayerKitChanged>.HandleEvent(PlayerKitChanged e, IServiceProvider serviceProvider)
    {
        UpdateFlag(e.Player);
    }

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        UpdateFlag(e.Player);
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        UpdateFlag(e.Player);
    }

    void IEventListener<PlayerEnteredZone>.HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
        UpdateFlag(e.Player);
    }

    void IEventListener<PlayerDeployed>.HandleEvent(PlayerDeployed e, IServiceProvider serviceProvider)
    {
        if (e.Destination is Zone { Type: ZoneType.WarRoom or ZoneType.MainBase })
        {
            UpdateFlag(e.Player);
        }
    }

    private void UpdateFlag(WarfarePlayer player)
    {
        if (player.Component<DeploymentComponent>().CurrentDeployment is Zone { Type: ZoneType.MainBase })
        {
            // already deploying
            player.SetFlag(_flagId, Flags.Deploying);
        }
        else if (player.Component<KitPlayerComponent>().ActiveClass <= Class.Unarmed)
        {
            // invalid kit
            player.SetFlag(_flagId, Flags.NoKit);
        }
        else if (!_zoneStore.IsInsideZone(player.Position, ZoneType.WarRoom, player.Team.Faction))
        {
            // not in war zone
            player.SetFlag(_flagId, Flags.AlreadyDeployed);
        }
        else
        {
            // all good
            player.SetFlag(_flagId, Flags.Open);
        }
    }
}