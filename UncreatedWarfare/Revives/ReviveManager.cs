using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Revives
{
    public class ReviveManager
    {
        private List<ulong> DownedPlayers;
        public ReviveManager()
        {

            DamageTool.damagePlayerRequested += OnPlayerDamagedRequested;
            PlayerStance.OnStanceChanged_Global += OnStanceChanged;

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
        }
        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested += OnEquipRequested;
        }
        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested -= OnEquipRequested;
        }
        private void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (!DownedPlayers.Contains(parameters.player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (parameters.damage > parameters.player.life.health && parameters.damage < 100)
                {
                    parameters.player.equipment.dequip();

                    parameters.player.life.ReceiveHealth(10);

                    parameters.player.stance.ReceiveStance(EPlayerStance.PRONE);

                    parameters.player.movement.sendPluginSpeedMultiplier(0.1F);
                    parameters.player.movement.sendPluginJumpMultiplier(0);

                    DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID);

                    Reviver reviver = UnturnedPlayer.FromPlayer(parameters.player).GetComponent<Reviver>();
                    reviver.StartBleedout(this, parameters);
                }
            }
            else
            {
                parameters.damage = parameters.damage * 0.1F;
            }
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            if (DownedPlayers.Contains(equipment.player.channel.owner.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
            }
        }
        private void OnStanceChanged(PlayerStance stance)
        {
            if (DownedPlayers.Contains(stance.player.channel.owner.playerID.steamID.m_SteamID))
            {
                stance.ReceiveStance(EPlayerStance.PRONE);
            }
        }
        private class Reviver : UnturnedPlayerComponent
        {
            public void StartBleedout(ReviveManager reviveManager, DamagePlayerParameters parameters)
            {
                StartCoroutine(WaitToKillPlayer(reviveManager, parameters));
            }
            private IEnumerator<WaitForSeconds> WaitToKillPlayer(ReviveManager reviveManager, DamagePlayerParameters parameters)
            {
                yield return new WaitForSeconds(10);

                if (reviveManager.DownedPlayers.Contains(parameters.player.channel.owner.playerID.steamID.m_SteamID))
                {
                    reviveManager.DownedPlayers.Remove(parameters.player.channel.owner.playerID.steamID.m_SteamID);

                    DamageTool.damagePlayer(
                            new DamagePlayerParameters(parameters.player)
                            {
                                cause = parameters.cause,
                                limb = parameters.limb,
                                killer = parameters.killer,
                                direction = parameters.direction,
                                damage = 100,
                                times = 1,
                                respectArmor = false,
                                applyGlobalArmorMultiplier = false,
                                bleedingModifier = 0,
                                bonesModifier = 0
                            }, out EPlayerKill kill);
                }
            }
        }
    }
}
