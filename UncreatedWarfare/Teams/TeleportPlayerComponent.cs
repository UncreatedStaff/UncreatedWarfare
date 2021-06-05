using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Teams
{
    public class PlayerTeleportedEventArgs : EventArgs 
    { public Player player; public Vector3 OldPosition; public Vector3 NewPosition; public bool WasInVehicle; public float Delay; }
    class TeleportPlayerComponent : UnturnedPlayerComponent
    {
        public event EventHandler<PlayerTeleportedEventArgs> PlayerTeleported;
        public void CancelTeleportRequests()
        {
            StopAllCoroutines();
        }
        public bool InstantlyTeleportPlayer(Vector3 Destination, bool KickOutOfVehicle) => TeleportPlayer(Destination, 0, KickOutOfVehicle);
        public void DelayedTeleportPlayer(Vector3 Destination, float Seconds, bool KickOutOfVehicle) => StartCoroutine(TeleportPlayerTimer(Destination, Seconds, KickOutOfVehicle));
        private bool TeleportPlayer(Vector3 Destination, float Seconds, bool KickOutOfVehicle)
        {
            bool success = true;
            bool wasInVehicle = false;
            InteractableVehicle vehicle = Player.Player.movement.getVehicle();
            if (vehicle != null) // if player is in a vehicle
            {
                wasInVehicle = true;
                if (KickOutOfVehicle)
                {
                    if (!Player.Player.movement.forceRemoveFromVehicle())
                    {
                        F.LogError("Unable to remove " + Player.Player.channel.owner.playerID.playerName +
                            $" from their vehicle for teleport to ({Destination.x}, {Destination.y}, {Destination.z}) with a delay of {Seconds} seconds.");
                        success = false;
                    }
                }
                else success = false;
            }
            if (success)
            {
                Vector3 OldPosition = Player.Player.transform.position;
                if (Player.Player.teleportToLocation(Destination, 0))
                {
                    if (PlayerTeleported != null)
                        PlayerTeleported.Invoke(this, new PlayerTeleportedEventArgs
                        { Delay = Seconds, OldPosition = OldPosition, NewPosition = Player.Player.transform.position, player = Player.Player, WasInVehicle = wasInVehicle });
                    return true;
                }
            }
            else
            {
                F.LogError("Unable to teleport " + Player.Player.channel.owner.playerID.playerName +
                    $" to ({Destination.x}, {Destination.y}, {Destination.z}) with a delay of {Seconds} seconds, they are in a vehicle.");
            }
            return false;
        }
        private IEnumerator TeleportPlayerTimer(Vector3 Destination, float Seconds, bool KickOutOfVehicle)
        {
            yield return new WaitForSeconds(Seconds);
            TeleportPlayer(Destination, Seconds, KickOutOfVehicle);
        }
    }
}
