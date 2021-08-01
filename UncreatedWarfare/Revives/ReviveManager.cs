using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare.Revives
{
    public class ReviveManager : IDisposable
    {
        public readonly Dictionary<ulong, DamagePlayerParameters> DownedPlayers;
        public readonly Dictionary<ulong, float> DistancesFromInitialShot;
        public readonly List<UCPlayer> Medics = new List<UCPlayer>();
        private Coroutine Updater;
        const float SIM_TIME = 0.16f;
        public ReviveManager()
        {
            DownedPlayers = new Dictionary<ulong, DamagePlayerParameters>();
            DistancesFromInitialShot = new Dictionary<ulong, float>();
            Medics = PlayerManager.OnlinePlayers.Where(x => x.KitName != null && x.KitName != string.Empty 
            && KitManager.KitExists(x.KitName, out Kit kit) && kit.Class == Kit.EClass.MEDIC).ToList();
            UCWarfare.I.OnPlayerDeathPostMessages += OnPlayerDeath;
            PlayerLife.OnRevived_Global += OnPlayerRespawned;
            UseableConsumeable.onPerformingAid += UseableConsumeable_onPerformingAid;
            EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
            foreach(SteamPlayer player in Provider.clients)
            {
                player.player.stance.onStanceUpdated += delegate
                {
                    StanceUpdatedLocal(player);
                };
                player.player.equipment.onEquipRequested += OnEquipRequested;
            }
            Updater = UCWarfare.I.StartCoroutine(UpdatePositions());
        }
        private void OnEffectButtonClicked(Player player, string buttonName)
        {
            if (buttonName == "GiveUp")
            {
                if (DownedPlayers.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out DamagePlayerParameters p))
                {
                    p.damage = 255f;
                    p.times = 1;
                    DamageTool.damagePlayer(p, out _); // kill the player because they pressed 'Give Up'

                    // player and Revive UI will be removed from list in OnDeath
                }
            }
        }
        private IEnumerator<WaitForSeconds> UpdatePositions()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                if (DownedPlayers.Count == 0) continue;
                UpdateInjuredMarkers();
            }
        }
        private void UseableConsumeable_onPerformingAid(Player healer, Player downed, ItemConsumeableAsset asset, ref bool shouldAllow)
        {
            UCPlayer medic = UCPlayer.FromPlayer(healer);
            if (medic == null)
            {
                shouldAllow = false;
                return;
            }
            if (medic.GetTeam() != downed.GetTeam())
            {
                medic.Message("heal_e_enemy");
                shouldAllow = false;
                return;
            }
            if (!DownedPlayers.ContainsKey(downed.channel.owner.playerID.steamID.m_SteamID)) // if not injured
                return;
            if (medic.KitClass != Kit.EClass.MEDIC)
            {
                medic.Message("heal_e_notmedic");
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
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            if (KitManager.KitExists(ucplayer.KitName, out Kit kit) && kit.Class == Kit.EClass.MEDIC)
                Medics.Add(ucplayer);
            DownedPlayers.Remove(player.CSteamID.m_SteamID);
            DistancesFromInitialShot.Remove(player.CSteamID.m_SteamID);
        }
        /// <summary>Pre-destroy</summary>
        internal void OnPlayerDisconnected(SteamPlayer player)
        {
            player.player.equipment.onEquipRequested -= OnEquipRequested;
            player.player.stance.onStanceUpdated -= delegate
            {
                StanceUpdatedLocal(player);
            };
            Medics.RemoveAll(x => x == null || x.Steam64 == player.playerID.steamID.m_SteamID);
            if (DownedPlayers.TryGetValue(player.playerID.steamID.m_SteamID, out DamagePlayerParameters p))
            {
                if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out PlayerSave save))
                {
                    save.ShouldRespawnOnJoin = true;
                    PlayerManager.Save();
                }
                p.damage = 255f;
                p.times = 1;
                UCWarfare.I.shouldwait = true; // make death message synchronous and not run and forget.
                player.player.life.askDamage(byte.MaxValue, Vector3.up, p.cause, p.limb, p.killer, out _, p.trackKill, p.ragdollEffect, false, true);
                //DamageTool.damagePlayer(p, out _); // kill the player if they're down and try to leave.
                
                // player will be removed from list in OnDeath
            }
        }
        internal async Task OnPlayerHealedAsync(Player medic, Player target)
        {
            if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
            {
                r.RevivePlayer();
                ulong team = medic.GetTeam();
                if (team == target.GetTeam())
                    await XPManager.AddXP(medic, team, XPManager.config.Data.FriendlyRevivedXP, 
                        F.Translate("xp_healed_teammate", medic.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(target).CharacterName));

                target.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
                EffectManager.askEffectClearByID(30600, target.channel.owner.transportConnection);
            }
        }
        internal void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (!DownedPlayers.ContainsKey(parameters.player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (parameters.player.life.health > 0 &&
                    !parameters.player.life.isDead &&
                    parameters.damage > parameters.player.life.health &&
                    ((parameters.damage < 80 * parameters.player.life.health && !(parameters.limb == ELimb.SKULL)) ||
                    (parameters.damage < 160 * parameters.player.life.health && !(parameters.limb == ELimb.SPINE)))
                    )
                {
                    InjurePlayer(ref shouldAllow, ref parameters);
                }
            }
            else
            {
                float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
                parameters.damage *= (UCWarfare.Config.InjuredDamageMultiplier / 10) * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
            }
        }
        private void InjurePlayer(ref bool shouldAllow, ref DamagePlayerParameters parameters)
        {
            shouldAllow = false;
            parameters.player.equipment.dequip();

            // times per second FixedUpdate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
            float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
            F.Log(bleedsPerSecond + " bleed times per second");
            parameters.player.life.serverModifyHealth(UCWarfare.Config.InjuredLifeTimeSeconds * bleedsPerSecond - parameters.player.life.health);
            parameters.player.life.serverSetBleeding(true);

            parameters.player.movement.sendPluginSpeedMultiplier(0.1F);
            parameters.player.movement.sendPluginJumpMultiplier(0);

            parameters.player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.sendUIEffect(30600, 30600, true);

            DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, parameters);
            if (parameters.killer != default && parameters.killer != CSteamID.Nil)
            {
                Player killer = PlayerTool.getPlayer(parameters.killer);
                if (killer != default)
                {
                    if (DistancesFromInitialShot.ContainsKey(parameters.player.channel.owner.playerID.steamID.m_SteamID))
                        DistancesFromInitialShot[parameters.player.channel.owner.playerID.steamID.m_SteamID] = Vector3.Distance(killer.transform.position, parameters.player.transform.position);
                    else
                        DistancesFromInitialShot.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, Vector3.Distance(killer.transform.position, parameters.player.transform.position));

                    if (killer.channel.owner.playerID.steamID.m_SteamID != parameters.player.channel.owner.playerID.steamID.m_SteamID) // suicide
                    {
                        if (killer.GetTeam() != parameters.player.GetTeam())
                            ToastMessage.QueueMessage(killer, "", F.Translate("xp_enemy_downed", killer), ToastMessageSeverity.MINIXP);
                        else
                            ToastMessage.QueueMessage(killer, "", F.Translate("xp_friendly_downed", killer), ToastMessageSeverity.MINIXP);
                    }
                }
            }
            if (parameters.player.transform.TryGetComponent(out Reviver reviver))
            {
                reviver.TellProneDelayed();
                //reviver.StartBleedout();
            }
        }
        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            //F.Log(player.Player.channel.owner.playerID.playerName + " died in ReviveManager.", ConsoleColor.DarkRed);
            if(DownedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                if (player.Player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(this, true);
                } else
                {
                    DownedPlayers.Remove(player.CSteamID.m_SteamID);
                    DistancesFromInitialShot.Remove(player.CSteamID.m_SteamID);
                }

                player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
                EffectManager.askEffectClearByID(30600, player.Player.channel.owner.transportConnection);
            }
            ClearInjuredMarker(player.CSteamID.m_SteamID, player.GetTeam());
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
            //F.Log(equipment.player.channel.owner.playerID.playerName + " tried to equip", ConsoleColor.DarkRed);
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
        public void RegisterMedic(UCPlayer player)
        {
            Medics.Add(player);
            Vector3[] newpositions = DownedPlayers.Keys.Select(x => UCPlayer.FromID(x).Position).ToArray();
            SpawnInjuredMarkers(player.Player.channel.owner.transportConnection, newpositions, true);
        }
        public void DeregisterMedic(UCPlayer player)
        {
            Medics.RemoveAll(x => x == null || x.Steam64 == player.Steam64);
            ClearInjuredMarkers(player);
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
            //Provider.onEnemyDisconnected -= OnPlayerDisconnected;
            if (Updater != null)
                UCWarfare.I.StopCoroutine(Updater);
            Updater = null;
        }
        public void SpawnInjuredMarker(Vector3 Position, ulong Team)
        {
            IEnumerator<ITransportConnection> player = Medics.Where(x => x.GetTeam() == Team)
                .Select(x => x.Player.channel.owner.transportConnection).GetEnumerator();
            while (player.MoveNext())
            {
                EffectManager.sendEffectReliable(Squads.SquadManager.config.Data.InjuredMarker, player.Current, Position);
            }
            player.Dispose();
        }
        public void SpawnInjuredMarkers(IEnumerator<ITransportConnection> players, Vector3[] positions, bool dispose, bool clearAll)
        {
            while (players.MoveNext())
            {
                if (clearAll)
                    EffectManager.askEffectClearByID(Squads.SquadManager.config.Data.InjuredMarker, players.Current);
                for (int i = 0; i < positions.Length; i++)
                    EffectManager.sendEffectReliable(Squads.SquadManager.config.Data.InjuredMarker, players.Current, positions[i]);
            }
            if (dispose) players.Dispose();
        }
        public void SpawnInjuredMarkers(ITransportConnection player, Vector3[] positions, bool clearAll)
        {
            if (clearAll)
                EffectManager.askEffectClearByID(Squads.SquadManager.config.Data.InjuredMarker, player);
            for (int i = 0; i < positions.Length; i++)
                EffectManager.sendEffectReliable(Squads.SquadManager.config.Data.InjuredMarker, player, positions[i]);
        }
        public void ClearInjuredMarker(ulong clearedPlayer, ulong Team)
        {
            IEnumerator<ITransportConnection> medics = Medics.Where(x => x.GetTeam() == Team)
                .Select(x => x.Player.channel.owner.transportConnection).GetEnumerator();
            Vector3[] newpositions = DownedPlayers.Keys.Where(x => x != clearedPlayer).Select(x => UCPlayer.FromID(x).Position).ToArray();
            SpawnInjuredMarkers(medics, newpositions, true, true);
        }
        public void ClearInjuredMarkers(UCPlayer medic)
        {
            EffectManager.askEffectClearByID(Squads.SquadManager.config.Data.InjuredMarker, medic.Player.channel.owner.transportConnection);
        }
        public void UpdateInjuredMarkers(ulong Team)
        {
            IEnumerator<ITransportConnection> medics = Medics.Where(x => x.GetTeam() == Team)
                .Select(x => x.Player.channel.owner.transportConnection).GetEnumerator();
            Vector3[] newpositions = DownedPlayers.Keys.Select(x => UCPlayer.FromID(x).Position).ToArray();
            SpawnInjuredMarkers(medics, newpositions, true, true);
        }
        public void UpdateInjuredMarkers()
        {
            IEnumerator<ITransportConnection> medics = Medics.Where(x => x.GetTeam() == 1).Select(x => x.Player.channel.owner.transportConnection).GetEnumerator();
            List<Vector3> newpositions = new List<Vector3>();
            foreach (ulong player in DownedPlayers.Keys)
            {
                UCPlayer pl = UCPlayer.FromID(player);
                if (pl == null) continue;
                if (pl.GetTeam() == 1)
                    newpositions.Add(pl.Position);
            }
            SpawnInjuredMarkers(medics, newpositions.ToArray(), true, true);
            medics = Medics.Where(x => x.GetTeam() == 2).Select(x => x.Player.channel.owner.transportConnection).GetEnumerator();
            newpositions.Clear();
            foreach (ulong player in DownedPlayers.Keys)
            {
                UCPlayer pl = UCPlayer.FromID(player);
                if (pl == null) continue;
                if (pl.GetTeam() == 2)
                    newpositions.Add(pl.Position);
            }
            SpawnInjuredMarkers(medics, newpositions.ToArray(), true, true);
        }
        private class Reviver : UnturnedPlayerComponent
        {
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
                //F.Log("Checked stance of " + Player.Player.channel.owner.playerID.playerName + " to " + stance.ToString() + ".", ConsoleColor.DarkRed);
                this.stance = null;
            }
            public static void TellStandDelayed(Player player, float time = 0.5f)
            {
                if (player.transform.TryGetComponent(out Reviver r))
                {
                    r.stance = player.StartCoroutine(r.WaitToChangeStance(EPlayerStance.STAND, time));
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
