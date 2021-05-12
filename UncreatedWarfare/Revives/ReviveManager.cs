using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace UncreatedWarfare.Revives
{
    public class ReviveManager
    {
        public readonly List<ulong> DownedPlayers;
        public ReviveManager()
        {
            DownedPlayers = new List<ulong>();

            DamageTool.damagePlayerRequested += OnPlayerDamagedRequested;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateStance += OnStanceChanged;

            DownedPlayers = new List<ulong>();

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
                    Logger.Log(parameters.player.channel.owner.playerID.characterName + " was downed");

                    shouldAllow = false;

                    parameters.player.equipment.dequip();

                    parameters.player.life.serverModifyHealth(10F - parameters.player.life.health);
                    parameters.player.life.serverSetBleeding(true);

                    parameters.player.movement.sendPluginSpeedMultiplier(0.1F);
                    parameters.player.movement.sendPluginJumpMultiplier(0);

                    DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID);

                    Reviver reviver = UnturnedPlayer.FromPlayer(parameters.player).GetComponent<Reviver>();
                    reviver.TellProne(parameters.player);
                    //reviver.StartBleedout(this, parameters);
                }
            }
            else
            {
                parameters.damage = parameters.damage * 0.1F;
            }
        }
        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            player.Player.movement.sendPluginSpeedMultiplier(1);
            player.Player.movement.sendPluginJumpMultiplier(1);

            DownedPlayers.Remove(player.CSteamID.m_SteamID);
        }

        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            if (DownedPlayers.Contains(equipment.player.channel.owner.playerID.steamID.m_SteamID))
            {   
                shouldAllow = false;
            }
        }
        private void OnStanceChanged(UnturnedPlayer player, byte stance)
        {
            Logger.Log(player.CharacterName + " tried to change stance");
            if (DownedPlayers.Contains(player.CSteamID.m_SteamID))
            {
                Reviver reviver = player.GetComponent<Reviver>();
                reviver.TellProne(player.Player);
            }
        }
        private class Reviver : UnturnedPlayerComponent
        {
            public void TellProne(Player player)
            {
                StartCoroutine(WaitToProne(player));
            }

            private IEnumerator<WaitForSeconds> WaitToProne(Player player)
            {
                yield return new WaitForSeconds(0.5F);
                player.stance.checkStance(EPlayerStance.PRONE, true);
            }

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

                    parameters.player.movement.sendPluginSpeedMultiplier(1);
                    parameters.player.movement.sendPluginJumpMultiplier(1);

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
