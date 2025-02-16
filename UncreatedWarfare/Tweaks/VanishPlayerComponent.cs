using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Manages the player's ability to be seen by other players.
/// </summary>
[PlayerComponent]
public class VanishPlayerComponent : IPlayerComponent
{
    private ZoneStore? _zoneStore;

    public static readonly PermissionLeaf VanishPermission = new PermissionLeaf("warfare::features.vanish");
    public bool IsActive
    {
        get => !Player.UnturnedPlayer.movement.canAddSimulationResultsToUpdates;
        set
        {
            Player.UnturnedPlayer.movement.canAddSimulationResultsToUpdates = !value;
            if (!value)
            {
                Player.UnturnedPlayer.movement.updates.Add(new PlayerStateUpdate(Player.Position, Player.UnturnedPlayer.look.angle, Player.UnturnedPlayer.look.rot));
                return;
            }

            Zone? lobbyZone = _zoneStore?.SearchZone(ZoneType.Lobby);

            if (lobbyZone == null)
            {
                Player.UnturnedPlayer.movement.updates.Add(new PlayerStateUpdate(Vector3.zero, 0, 0));
                return;
            }

            Vector3 pos = lobbyZone.Spawn;
            float angle = lobbyZone.SpawnYaw;
            Player.UnturnedPlayer.movement.updates.Add(new PlayerStateUpdate(pos, 0, MeasurementTool.angleToByte(angle)));
        }
    }

    public WarfarePlayer Player { get; private set; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}