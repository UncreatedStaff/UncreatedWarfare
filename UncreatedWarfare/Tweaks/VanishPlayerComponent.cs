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
            PlayerMovement movement = Player.UnturnedPlayer.movement;
            movement.canAddSimulationResultsToUpdates = !value;
            if (!value)
            {
                movement.updates.Add(new PlayerStateUpdate(Player.Position, Player.UnturnedPlayer.look.angle, Player.UnturnedPlayer.look.rot));
                return;
            }

            Zone? lobbyZone = _zoneStore?.SearchZone(ZoneType.Lobby);

            if (lobbyZone == null)
            {
                movement.updates.Add(new PlayerStateUpdate(Vector3.zero, 0, 0));
                return;
            }

            Vector3 pos = lobbyZone.Spawn;
            float angle = lobbyZone.SpawnYaw;
            movement.updates.Add(new PlayerStateUpdate(pos, 0, MeasurementTool.angleToByte(angle)));
        }
    }

    public required WarfarePlayer Player { get; init; }
    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
    }
}