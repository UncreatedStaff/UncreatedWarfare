using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UncreatedWarfare.Revives
{
    public class ReviveManager : IDisposable
    {
        public readonly Dictionary<ulong, DamagePlayerParameters> DownedPlayers;
        public readonly Dictionary<ulong, float> DistancesFromInitialShot;
        public ReviveManager()
        {
            DownedPlayers = new Dictionary<ulong, DamagePlayerParameters>();
            DistancesFromInitialShot = new Dictionary<ulong, float>();

            DamageTool.damagePlayerRequested += OnPlayerDamagedRequested;
            UCWarfare.I.OnPlayerDeathPostMessages += OnPlayerDeath;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateStance += OnStanceChanged;
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            foreach(SteamPlayer player in Provider.clients)
            {
                player.player.equipment.onEquipRequested += OnEquipRequested;
            }
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
            if (!DownedPlayers.ContainsKey(parameters.player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (parameters.damage > parameters.player.life.health && parameters.damage < 100)
                {
                    F.Log(parameters.player.channel.owner.playerID.characterName + " was downed");

                    shouldAllow = false;

                    parameters.player.equipment.dequip();

                    parameters.player.life.serverModifyHealth(10F - parameters.player.life.health);
                    parameters.player.life.serverSetBleeding(true);

                    parameters.player.movement.sendPluginSpeedMultiplier(0.1F);
                    parameters.player.movement.sendPluginJumpMultiplier(0);

                    DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, parameters);
                    if(parameters.killer != default && parameters.killer != CSteamID.Nil)
                    {
                        Player killer = PlayerTool.getPlayer(parameters.killer);
                        if(killer != default)
                        {
                            if (DistancesFromInitialShot.ContainsKey(parameters.player.channel.owner.playerID.steamID.m_SteamID))
                                DistancesFromInitialShot[parameters.player.channel.owner.playerID.steamID.m_SteamID] = Vector3.Distance(killer.transform.position, parameters.player.transform.position);
                            else
                                DistancesFromInitialShot.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, Vector3.Distance(killer.transform.position, parameters.player.transform.position));
                        }
                    }

                    Reviver reviver = UnturnedPlayer.FromPlayer(parameters.player).GetComponent<Reviver>();
                    reviver.TellProneDelayed();
                    reviver.StartBleedout(this);
                }
            }
            else
            {
                parameters.damage *= 0.1f;
            }
        }
        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            player.Player.movement.sendPluginSpeedMultiplier(1);
            player.Player.movement.sendPluginJumpMultiplier(1);
            Reviver.TellStandDelayed(player.Player);
            DownedPlayers.Remove(player.CSteamID.m_SteamID);
            DistancesFromInitialShot.Remove(player.CSteamID.m_SteamID);
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            if (DownedPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
            {   
                shouldAllow = false;
            }
        }
        private void OnStanceChanged(UnturnedPlayer player, byte stance)
        {
            F.Log(player.CharacterName + " tried to change stance");
            if (DownedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                if (player.Player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.TellProneDelayed();
                }
            }
        }

        public void Dispose()
        {
            foreach(DamagePlayerParameters paramaters in DownedPlayers.Values)
            {
                if (paramaters.player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(this);
                    reviver.TellStandDelayed();
                }
            }
            foreach(SteamPlayer player in Provider.clients)
            {
                player.player.equipment.onEquipRequested -= OnEquipRequested;
            }
            DamageTool.damagePlayerRequested -= OnPlayerDamagedRequested;
            UCWarfare.I.OnPlayerDeathPostMessages -= OnPlayerDeath;
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdateStance -= OnStanceChanged;
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
        }

        private class Reviver : UnturnedPlayerComponent
        {
            public void TellProneDelayed(float time = 0.5f)
            {
                StartCoroutine(WaitToChangeStance(EPlayerStance.PRONE, time));
            }
            public void TellStandDelayed(float time = 0.5f)
            {
                StartCoroutine(WaitToChangeStance(EPlayerStance.STAND, time));
            }
            private IEnumerator<WaitForSeconds> WaitToChangeStance(EPlayerStance stance, float time = 0.5f)
            {
                yield return new WaitForSeconds(time);
                Player.Player.stance.checkStance(stance, true);
            }
            public static void TellStandDelayed(Player player, float time = 0.5f)
            {
                if(player.transform.TryGetComponent(out Reviver r))
                {
                    player.StartCoroutine(r.WaitToChangeStance(EPlayerStance.STAND, time));
                }
            }
            public void StartBleedout(ReviveManager reviveManager)
            {
                StartCoroutine(WaitToKillPlayer(reviveManager));
            }
            private IEnumerator<WaitForSeconds> WaitToKillPlayer(ReviveManager reviveManager)
            {
                yield return new WaitForSeconds(10);

                if (reviveManager.DownedPlayers.ContainsKey(Player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    FinishKillingPlayer(reviveManager);
                } // else player already died
            }
            public void FinishKillingPlayer(ReviveManager reviveManager)
            {
                Player.Player.movement.sendPluginSpeedMultiplier(1);
                Player.Player.movement.sendPluginJumpMultiplier(1);

                DamagePlayerParameters parameters = reviveManager.DownedPlayers[Player.Player.channel.owner.playerID.steamID.m_SteamID];
                parameters.damage = 100;
                parameters.respectArmor = false;
                parameters.applyGlobalArmorMultiplier = false;
                DamageTool.damagePlayer(parameters, out _);
                reviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                reviveManager.DistancesFromInitialShot.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
}
