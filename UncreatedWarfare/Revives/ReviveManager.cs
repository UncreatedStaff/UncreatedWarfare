using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare.Revives
{
    public class ReviveManager : IDisposable
    {
        public readonly Dictionary<ulong, DamagePlayerParameters> DownedPlayers;
        public readonly Dictionary<ulong, float> DistancesFromInitialShot;
        public ReviveManager()
        {
            DownedPlayers = new Dictionary<ulong, DamagePlayerParameters>();
            DistancesFromInitialShot = new Dictionary<ulong, float>();

            UCWarfare.I.OnPlayerDeathPostMessages += OnPlayerDeath;
            PlayerLife.OnRevived_Global += OnPlayerRespawned;
            UseableConsumeable.onPerformingAid += UseableConsumeable_onPerformingAid;
            foreach(SteamPlayer player in Provider.clients)
            {
                player.player.stance.onStanceUpdated += delegate
                {
                    StanceUpdatedLocal(player);
                };
                player.player.equipment.onEquipRequested += OnEquipRequested;
            }
        }

        private void UseableConsumeable_onPerformingAid(Player healer, Player downed, ItemConsumeableAsset asset, ref bool shouldAllow)
        {
            UCPlayer medic = UCPlayer.FromPlayer(healer);

            if (medic.KitClass != Kits.Kit.EClass.MEDIC)
            {
                medic.Message("heal_e_notmedic");
                shouldAllow = false;
                return;
            }

            if (medic.GetTeam() != downed.quests.groupID.m_SteamID)
            {
                medic.Message("heal_e_enemy");
                shouldAllow = false;
                return;
            }
        }

        private void OnPlayerRespawned(PlayerLife obj)
        {
            if(obj.player.transform.TryGetComponent(out Reviver r))
                r.TellStanceNoDelay(EPlayerStance.STAND);
        }

        internal void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested += OnEquipRequested;
            player.Player.stance.onStanceUpdated += delegate
            {
                StanceUpdatedLocal(player.Player.channel.owner);
            };
        }
        internal void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Player.equipment.onEquipRequested -= OnEquipRequested;
            player.Player.stance.onStanceUpdated += delegate
            {
                StanceUpdatedLocal(player.Player.channel.owner);
            };
        }
        internal async Task OnPlayerHealedAsync(Player medic, Player target)
        {
            if (target.TryGetComponent(out Reviver r))
            {
                r.RevivePlayer();
                await XPManager.AddXP(medic, medic.GetTeam(), XPManager.config.Data.FriendlyRevivedXP);
            }
        }
        internal void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            F.Log(parameters.player.channel.owner.playerID.playerName + " took " + parameters.damage.ToString(Data.Locale) + " damage.", ConsoleColor.DarkRed);
            if (!DownedPlayers.ContainsKey(parameters.player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (parameters.damage > parameters.player.life.health && parameters.limb != ELimb.SKULL && parameters.player.life.health > 0 && !parameters.player.life.isDead && parameters.damage < 100)
                {
                    F.Log(parameters.player.channel.owner.playerID.characterName + " was downed.", ConsoleColor.DarkRed);

                    shouldAllow = false;

                    parameters.player.equipment.dequip();

                    parameters.player.life.serverModifyHealth(UCWarfare.Config.ReviveTimeSeconds - parameters.player.life.health);
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

                    if(parameters.player.transform.TryGetComponent(out Reviver reviver))
                    {
                        reviver.TellProneDelayed();
                        reviver.StartBleedout();
                    }
                }
            }
            else
            {
                parameters.damage *= 0.1f;
            }
        }
        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            F.Log(player.Player.channel.owner.playerID.playerName + " died in ReviveManager.", ConsoleColor.DarkRed);
            if(DownedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                if (player.Player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(this, true);
                }
            }
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            F.Log(equipment.player.channel.owner.playerID.playerName + " tried to equip", ConsoleColor.DarkRed);
            if (DownedPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
            {   
                shouldAllow = false;
            }
        }
        private void StanceUpdatedLocal(SteamPlayer player)
        {
            if (DownedPlayers.ContainsKey(player.playerID.steamID.m_SteamID) && player.player.transform.TryGetComponent(out Reviver reviver))
            {
                reviver.TellStanceNoDelay(EPlayerStance.PRONE);
            }
        }
        public void Dispose()
        {
            foreach(DamagePlayerParameters paramaters in DownedPlayers.Values)
            {
                if (paramaters.player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(this);
                }
            }
            foreach (SteamPlayer player in Provider.clients)
            {
                player.player.equipment.onEquipRequested -= OnEquipRequested;
                player.player.stance.onStanceUpdated = null;
            }
            UCWarfare.I.OnPlayerDeathPostMessages -= OnPlayerDeath;
            PlayerLife.OnRevived_Global -= OnPlayerRespawned;
        }
        private class Reviver : UnturnedPlayerComponent
        {
            private Coroutine bleedout;
            private Coroutine stance;
            public void TellProneDelayed(float time = 0.5f)
            {
                stance = StartCoroutine(WaitToChangeStance(EPlayerStance.PRONE, time));
            }
            public void TellStanceNoDelay(EPlayerStance stance)
            {
                Player.Player.stance.checkStance(stance, true);
            }
            private IEnumerator<WaitForSeconds> WaitToChangeStance(EPlayerStance stance, float time = 0.5f)
            {
                yield return new WaitForSeconds(time);
                TellStanceNoDelay(stance);
                F.Log("Checked stance of " + Player.Player.channel.owner.playerID.playerName + " to " + stance.ToString() + ".", ConsoleColor.DarkRed);
                this.stance = null;
            }
            public static void TellStandDelayed(Player player, float time = 0.5f)
            {
                if (player.transform.TryGetComponent(out Reviver r))
                {
                    r.stance = player.StartCoroutine(r.WaitToChangeStance(EPlayerStance.STAND, time));
                }
            }
            public void StartBleedout()
            {
                bleedout = StartCoroutine(WaitToKillPlayer());
            }
            private IEnumerator<WaitForSeconds> WaitToKillPlayer()
            {
                yield return new WaitForSeconds(10);
                F.Log(Player.Player.channel.owner.playerID.playerName + " bled out or something.", ConsoleColor.DarkRed);
                bleedout = null;
                /*
                if (reviveManager.DownedPlayers.ContainsKey(Player.Player.channel.owner.playerID.steamID.m_SteamID))
                {
                    FinishKillingPlayer(reviveManager);
                } // else player already died
                */
            }
            public void CancelBleedout()
            {
                if (bleedout != null)
                {
                    StopCoroutine(bleedout);
                    bleedout = null;
                }
            }
            public void CancelStance()
            {
                if (stance != null)
                {
                    StopCoroutine(stance);
                    stance = null;
                }
            }
            public void RevivePlayer() => RevivePlayer(Data.ReviveManager);
            public void RevivePlayer(ReviveManager reviveManager, bool remove = true)
            {
                Player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                Player.Player.movement.sendPluginJumpMultiplier(1.0f);
                Player.Player.life.serverSetBleeding(false);
                CancelBleedout();
                CancelStance();
                if(remove)
                {
                    reviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                    reviveManager.DistancesFromInitialShot.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
            }
            public void FinishKillingPlayer(bool isDead = false) => FinishKillingPlayer(Data.ReviveManager, isDead);
            public void FinishKillingPlayer(ReviveManager reviveManager, bool isDead = false)
            {
                this.RevivePlayer(reviveManager, false);
                if(!isDead)
                {
                    DamagePlayerParameters parameters = reviveManager.DownedPlayers[Player.Player.channel.owner.playerID.steamID.m_SteamID];
                    parameters.damage = 100.0f;
                    parameters.respectArmor = false;
                    parameters.applyGlobalArmorMultiplier = false;
                    DamageTool.damagePlayer(parameters, out _);
                }
                reviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                reviveManager.DistancesFromInitialShot.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
}
