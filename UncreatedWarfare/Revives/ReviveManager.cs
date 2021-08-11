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
        public readonly Dictionary<ulong, DeathInfo> DeathInfo;
        public readonly List<UCPlayer> Medics = new List<UCPlayer>();
        private Coroutine Updater;
        const float SIM_TIME = 0.08f;
        const bool CAN_HEAL_ENEMIES = true;
        public ReviveManager()
        {
            DownedPlayers = new Dictionary<ulong, DamagePlayerParameters>();
            DeathInfo = new Dictionary<ulong, DeathInfo>();
            Medics = PlayerManager.OnlinePlayers.Where(x => x.KitName != null && x.KitName != string.Empty 
            && KitManager.KitExists(x.KitName, out Kit kit) && kit.Class == Kit.EClass.MEDIC).ToList();
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
            Updater = UCWarfare.I.StartCoroutine(UpdatePositions());
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
            if (!CAN_HEAL_ENEMIES || medic.GetTeam() != downed.GetTeam())
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
            if (obj.player.TryGetComponent(out Reviver r))
                r.TellStandDelayed(1.5f);
            obj.player.movement.sendPluginSpeedMultiplier(1.0f);
            obj.player.movement.sendPluginJumpMultiplier(1.0f);
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
            DeathInfo.Remove(player.CSteamID.m_SteamID);
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
                player.player.life.askDamage(byte.MaxValue, Vector3.up, p.cause, p.limb, p.killer, out _, p.trackKill, p.ragdollEffect, false, true);
                // player will be removed from list in OnDeath
            }
        }
        internal void SetStanceBetter(Player player, EPlayerStance stance)
        {
            if (Data.PrivateStance == null || Data.ReplicateStance == null)
            {
                player.stance.checkStance(stance);
                F.LogWarning("Unable to set stance properly, fell back to checkStance.");
            }
            Data.PrivateStance.SetValue(player.stance, stance);
            Data.ReplicateStance.Invoke(player.stance, new object[] { false });
        }
        internal void OnPlayerHealed(Player medic, Player target)
        {
            if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
            {
                r.RevivePlayer();
                ulong team = medic.GetTeam();
                ulong tteam = target.GetTeam();
                if (team == tteam)
                {
                    XPManager.AddXP(medic, team, XPManager.config.Data.FriendlyRevivedXP,
                        F.Translate("xp_healed_teammate", medic.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(target).CharacterName));
                    if (medic.TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats != null)
                        c.stats.revives++;
                }
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, target.channel.owner.transportConnection);
                ClearInjuredMarker(target.channel.owner.playerID.steamID.m_SteamID, tteam);
            }
        }
        public void RevivePlayer(Player target)
        {
            if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
            {
                r.RevivePlayer();
                ulong team = target.GetTeam();
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, target.channel.owner.transportConnection);
                ClearInjuredMarker(target.channel.owner.playerID.steamID.m_SteamID, team);
            }
        }
        internal void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (Data.Gamemode.State != Gamemodes.EState.ACTIVE)
            {
                shouldAllow = false;
                return;
            }
            if (!DownedPlayers.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DamagePlayerParameters p))
            {
                SteamPlayer killer = PlayerTool.getSteamPlayer(parameters.killer);
                if (killer != null && DownedPlayers.ContainsKey(killer.playerID.steamID.m_SteamID))
                {
                    shouldAllow = false;
                    return;
                }
                if (UCWarfare.Config.Debug)
                    F.Log(parameters.player.name + " took " + parameters.damage + " damage in the " + parameters.limb.ToString() + " while not downed.", ConsoleColor.DarkGray);

                if (!parameters.player.life.isDead &&
                    parameters.damage > parameters.player.life.health &&
                    parameters.damage < 220)
                {
                    InjurePlayer(ref shouldAllow, ref parameters, killer);
                }
            }
            else
            {
                float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
                parameters = p;
                parameters.damage *= (UCWarfare.Config.InjuredDamageMultiplier / 10) * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
                if (UCWarfare.Config.Debug)
                    F.Log(parameters.player.name + " took " + parameters.damage + " damage in the " + parameters.limb.ToString() + " while downed.", ConsoleColor.DarkGray);
            }
        }
        private void InjurePlayer(ref bool shouldAllow, ref DamagePlayerParameters parameters, SteamPlayer killer)
        {
            if (parameters.player.movement.getVehicle() != null || parameters.cause == EDeathCause.VEHICLE)
                return;
            shouldAllow = false;
            parameters.player.equipment.dequip();

            // times per second FixedUpdate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
            float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
            //F.Log(bleedsPerSecond + " bleed times per second");
            parameters.player.life.serverModifyHealth(UCWarfare.Config.InjuredLifeTimeSeconds * bleedsPerSecond - parameters.player.life.health);
            parameters.player.life.serverSetBleeding(true);

            parameters.player.movement.sendPluginSpeedMultiplier(0.1f);
            parameters.player.movement.sendPluginJumpMultiplier(0);
            EffectManager.sendUIEffect(UCWarfare.Config.GiveUpUI, unchecked((short)UCWarfare.Config.GiveUpUI),
                parameters.player.channel.owner.transportConnection, true, F.Translate("injured_ui_header", parameters.player),
                F.Translate("injured_ui_give_up", parameters.player));
            parameters.player.SendChat("injured_chat");

            DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, parameters);
            SpawnInjuredMarker(parameters.player.transform.position, parameters.player.GetTeam());
            if (killer != default)
            {
                if (DeathInfo.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DeathInfo info))
                {
                    UCWarfare.I.GetKillerInfo(out info.item, out info.distance, out info.killerName, out info.killerTeam, parameters.cause, killer, parameters.player);
                }
                else
                {
                    UCWarfare.I.GetKillerInfo(out ushort item, out float distance, out FPlayerName names, out ulong killerTeam, parameters.cause, killer, parameters.player);
                    DeathInfo.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID,
                        new DeathInfo()
                        {
                            distance = distance,
                            item = item,
                            killerName = names,
                            killerTeam = killerTeam
                        });
                }
                if (killer.playerID.steamID.m_SteamID != parameters.player.channel.owner.playerID.steamID.m_SteamID) // suicide
                {
                    if (killer.GetTeam() != parameters.player.GetTeam())
                        ToastMessage.QueueMessage(killer, "", F.Translate("xp_enemy_downed", killer), ToastMessageSeverity.MINIXP);
                    else
                        ToastMessage.QueueMessage(killer, "", F.Translate("xp_friendly_downed", killer), ToastMessageSeverity.MINIXP);
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
            SetStanceBetter(player.Player, EPlayerStance.STAND);
            if (DownedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                if (player.Player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(this, true);
                }
                else
                {
                    DownedPlayers.Remove(player.CSteamID.m_SteamID);
                    DeathInfo.Remove(player.CSteamID.m_SteamID);
                    player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                    player.Player.movement.sendPluginJumpMultiplier(1.0f);
                    player.Player.life.serverSetBleeding(false);
                }

                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, player.Player.channel.owner.transportConnection);
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
            Vector3[] newpositions = GetPositionsOfTeam(player.GetTeam());
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
            IEnumerator<ITransportConnection> player = Medics
                .Where(x => x.GetTeam() == Team)
                .Select(x => x.Player.channel.owner.transportConnection)
                .GetEnumerator();
            while (player.MoveNext())
            {
                EffectManager.sendEffectReliable(Squads.SquadManager.config.Data.InjuredMarker, player.Current, Position);
            }
            player.Dispose();
        }
        internal void GiveUp(Player player)
        {
            if (DownedPlayers.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out DamagePlayerParameters p))
            {
                player.life.askDamage(byte.MaxValue, Vector3.down, p.cause, p.limb, p.killer, out _, p.trackKill, p.ragdollEffect, false, true);
                // player and Revive UI will be removed from list in OnDeath
            }
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
            IEnumerator<ITransportConnection> medics = Medics
                .Where(x => x.GetTeam() == Team)
                .Select(x => x.Player.channel.owner.transportConnection)
                .GetEnumerator();
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
        public Vector3[] GetPositionsOfTeam(ulong Team) =>
                DownedPlayers
                .Where(x => x.Value.player.GetTeam() == Team)
                .Select(x => UCPlayer.FromID(x.Key).Position)
                .ToArray();
        public void UpdateInjuredMarkers()
        {
            IEnumerator<ITransportConnection> medics = Medics.
                Where(x => x.GetTeam() == 1)
                .Select(x => x.Player.channel.owner.transportConnection)
                .GetEnumerator();
            Vector3[] newpositions = GetPositionsOfTeam(1);
            SpawnInjuredMarkers(medics, newpositions, true, true);
            medics = Medics
                .Where(x => x.GetTeam() == 2)
                .Select(x => x.Player.channel.owner.transportConnection)
                .GetEnumerator();
            newpositions = GetPositionsOfTeam(2);
            SpawnInjuredMarkers(medics, newpositions, true, true);
        }
        private class Reviver : UnturnedPlayerComponent
        {
            private Coroutine stance;
#pragma warning disable IDE0051
            void Start()
            {
                Player.Player.life.onHurt += OnPlayerPostDamage;
                Player.Player.inventory.onDropItemRequested += EventFunctions.OnDropItemTry;
            }
            void OnDisable()
            {
                Player.Player.life.onHurt -= OnPlayerPostDamage;
                Player.Player.inventory.onDropItemRequested -= EventFunctions.OnDropItemTry;
            }
#pragma warning restore IDE0051
            private void OnPlayerPostDamage(Player player, byte damage, Vector3 force, EDeathCause cause, ELimb limb, CSteamID killerid)
            {
                if (F.TryGetPlaytimeComponent(killerid, out Components.PlaytimeComponent c) && c.stats != null)
                {
                    c.stats.damagedone += damage;
                }
            }

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
            public void TellStandDelayed(float time = 0.5f)
            {
                stance = StartCoroutine(WaitToChangeStance(EPlayerStance.STAND, time));
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
                    reviveManager.DeathInfo.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
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
                reviveManager.DeathInfo.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
        }
    }
}
