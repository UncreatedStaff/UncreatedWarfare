using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using UnityEngine;

namespace Uncreated.Warfare.Revives
{
    public class ReviveManager : IDisposable
    {
        public readonly Dictionary<ulong, DownedPlayerData> DownedPlayers;
        public readonly Dictionary<ulong, DeathInfo> DeathInfo;
        public readonly List<UCPlayer> Medics = new List<UCPlayer>();
        private Coroutine? Updater;
        const float SIM_TIME = 0.08f;
        const bool CAN_HEAL_ENEMIES = true;
        public ReviveManager()
        {
            DownedPlayers = new Dictionary<ulong, DownedPlayerData>();
            DeathInfo = new Dictionary<ulong, DeathInfo>();
            Medics = PlayerManager.OnlinePlayers.Where(x => x.KitName != null && x.KitName != string.Empty
            && KitManager.KitExists(x.KitName, out Kit kit) && kit.Class == EClass.MEDIC).ToList();
            UCWarfare.I.OnPlayerDeathPostMessages += OnPlayerDeath;
            PlayerLife.OnRevived_Global += OnPlayerRespawned;
            UseableConsumeable.onPerformingAid += UseableConsumeable_onPerformingAid;
            foreach (SteamPlayer player in Provider.clients)
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
                yield return new WaitForSeconds(0.5f);
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (DownedPlayers.Count == 0) continue;
                UpdateInjuredMarkers();
                UpdateMedicMarkers();
            }
        }
        private void UseableConsumeable_onPerformingAid(Player healer, Player downed, ItemConsumeableAsset asset, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? medic = UCPlayer.FromPlayer(healer);
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
            if (medic.KitClass != EClass.MEDIC)
            {
                medic.Message("heal_e_notmedic");
                shouldAllow = false;
                return;
            }
        }

        private void OnPlayerRespawned(PlayerLife obj)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (obj.player.TryGetComponent(out Reviver r))
                r.TellStandDelayed(1.5f);
            obj.player.movement.sendPluginSpeedMultiplier(1.0f);
            obj.player.movement.sendPluginJumpMultiplier(1.0f);
        }

        internal void OnPlayerConnected(UCPlayer ucplayer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ucplayer.Player.equipment.onEquipRequested += OnEquipRequested;
            ucplayer.Player.stance.onStanceUpdated += delegate
            {
                StanceUpdatedLocal(ucplayer.Player.channel.owner);
            };
            if (KitManager.KitExists(ucplayer.KitName, out Kit kit) && kit.Class == EClass.MEDIC)
                Medics.Add(ucplayer);
            DownedPlayers.Remove(ucplayer.CSteamID.m_SteamID);
            DeathInfo.Remove(ucplayer.CSteamID.m_SteamID);
        }
        /// <summary>Pre-destroy</summary>
        internal void OnPlayerDisconnected(SteamPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            player.player.equipment.onEquipRequested -= OnEquipRequested;
            player.player.stance.onStanceUpdated -= delegate
            {
                StanceUpdatedLocal(player);
            };
            Medics.RemoveAll(x => x == null || x.Steam64 == player.playerID.steamID.m_SteamID);
            if (DownedPlayers.TryGetValue(player.playerID.steamID.m_SteamID, out DownedPlayerData p))
            {
                if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out PlayerSave save))
                {
                    save.ShouldRespawnOnJoin = true;
                    PlayerManager.ApplyToOnline();
                }
                player.player.life.askDamage(byte.MaxValue, Vector3.up, p.parameters.cause, p.parameters.limb, p.parameters.killer, out _, p.parameters.trackKill, p.parameters.ragdollEffect, false, true);
                // player will be removed from list in OnDeath
            }
        }
        internal void SetStanceBetter(Player player, EPlayerStance stance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.PrivateStance == null || Data.ReplicateStance == null)
            {
                player.stance.checkStance(stance);
                L.LogWarning("Unable to set stance properly, fell back to checkStance.");
            }
            Data.PrivateStance?.SetValue(player.stance, stance);
            Data.ReplicateStance?.Invoke(player.stance, new object[] { false });
        }
        internal void OnPlayerHealed(Player medic, Player target)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (target.TryGetComponent(out Reviver r) && DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
            {
                ActionLog.Add(EActionLogType.REVIVED_PLAYER, target.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale), medic.channel.owner.playerID.steamID.m_SteamID);
                r.RevivePlayer(null);
                byte team = medic.GetTeamByte();
                byte tteam = target.GetTeamByte();
                if (team == tteam)
                {
                    // TODO: better points calculations
                    UCPlayer? ucmedic = UCPlayer.FromPlayer(medic);
                    UCPlayer? uctarget = UCPlayer.FromPlayer(target);
                    if (ucmedic != null)
                    {
                        Points.AwardXP(ucmedic, Points.XPConfig.FriendlyRevivedXP, Translation.Translate("xp_healed_teammate", medic.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(target).CharacterName));
                        if (uctarget != null)
                            QuestManager.OnRevive(ucmedic, uctarget);
                    }


                    if (medic.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IRevivesStats r2)
                        r2.AddRevive();

                    Stats.StatsManager.ModifyTeam(team, t => t.Revives++, false);
                    if (KitManager.HasKit(medic, out Kit kit))
                    {
                        Stats.StatsManager.ModifyStats(medic.channel.owner.playerID.steamID.m_SteamID, s =>
                        {
                            s.Revives++;
                            Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                            if (kitData == default)
                            {
                                kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = team, Revives = 1 };
                                s.Kits.Add(kitData);
                            }
                            else
                            {
                                kitData.Revives++;
                            }
                        }, false);
                    }
                    else
                        Stats.StatsManager.ModifyStats(medic.channel.owner.playerID.steamID.m_SteamID, s => s.Revives++, false);
                }
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, target.channel.owner.transportConnection);
                EffectManager.askEffectClearByID(Squads.SquadManager.config.data.MedicMarker, target.channel.owner.transportConnection);
                ClearInjuredMarker(target.channel.owner.playerID.steamID.m_SteamID, tteam);
            }
        }
        public void RevivePlayer(Player target)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (DownedPlayers.ContainsKey(target.channel.owner.playerID.steamID.m_SteamID))
            {
                if (target.TryGetComponent(out Reviver r))
                    r.RevivePlayer();
            }
        }
        internal void OnPlayerDamagedRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Data.Gamemode.State != EState.ACTIVE)
            {
                shouldAllow = false;
                return;
            }
            if (Data.Gamemode is ITeams)
            {
                if (Teams.TeamManager.LobbyZone != null && Teams.TeamManager.LobbyZone.IsInside(parameters.player.transform.position))
                    return;
            }
            if (!DownedPlayers.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
            {
                SteamPlayer? killer = PlayerTool.getSteamPlayer(parameters.killer);
                if (killer != null && DownedPlayers.ContainsKey(killer.playerID.steamID.m_SteamID))
                {
                    shouldAllow = false;
                    return;
                }
                UCPlayer? player = UCPlayer.FromPlayer(parameters.player);
                if (player != null && player.OnDuty())
                {
                    if (parameters.player.TryGetComponent(out UnturnedPlayerFeatures features) && features.GodMode)
                    {
                        shouldAllow = false;
                        return;
                    }
                }

                if (!parameters.player.life.isDead &&
                    parameters.damage > parameters.player.life.health &&
                    parameters.cause != EDeathCause.LANDMINE &&
                    parameters.damage < 300)
                {
                    InjurePlayer(ref shouldAllow, ref parameters, killer);
                }
            }
            else
            {
                if ((DateTime.Now - p.start).TotalSeconds >= 0.4)
                {
                    float bleedsPerSecond = Time.timeScale / SIM_TIME / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
                    parameters = p.parameters;
                    parameters.damage *= UCWarfare.Config.InjuredDamageMultiplier / 10 * bleedsPerSecond * UCWarfare.Config.InjuredLifeTimeSeconds;
                }
                else
                {
                    shouldAllow = false;
                }
            }
        }
        private void InjurePlayer(ref bool shouldAllow, ref DamagePlayerParameters parameters, SteamPlayer? killer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!shouldAllow)
                return;
            if (parameters.player.movement.getVehicle() != null || parameters.cause == EDeathCause.VEHICLE)
                return;
            shouldAllow = false;
            parameters.player.equipment.dequip();

            // times per second FixedUpdate() is ran times bleed damage ticks = how many seconds it will take to lose 1 hp
            float bleedsPerSecond = (Time.timeScale / SIM_TIME) / Provider.modeConfigData.Players.Bleed_Damage_Ticks;
            //L.Log(bleedsPerSecond + " bleed times per second");
            parameters.player.life.serverModifyHealth(UCWarfare.Config.InjuredLifeTimeSeconds * bleedsPerSecond - parameters.player.life.health);
            parameters.player.life.serverSetBleeding(true);
            ulong team = parameters.player.GetTeam();
            parameters.player.movement.sendPluginSpeedMultiplier(0.35f);
            parameters.player.movement.sendPluginJumpMultiplier(0);
            short key = unchecked((short)UCWarfare.Config.GiveUpUI);
            EffectManager.sendUIEffect(UCWarfare.Config.GiveUpUI, key, parameters.player.channel.owner.transportConnection, true, Translation.Translate("injured_ui_header", parameters.player), string.Empty);
            EffectManager.sendUIEffectText(key, parameters.player.channel.owner.transportConnection, true, "GiveUpText", Translation.Translate("injured_ui_give_up", parameters.player));
            parameters.player.SendChat("injured_chat");

            ActionLog.Add(EActionLogType.INJURED, "by " + (killer == null ? "self" : killer.playerID.steamID.m_SteamID.ToString(Data.Locale)), parameters.player.channel.owner.playerID.steamID.m_SteamID);

            DownedPlayers.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID, new DownedPlayerData(parameters));
            SpawnInjuredMarker(parameters.player.transform.position, team);
            UpdateMedicMarkers(parameters.player.channel.owner.transportConnection, team, parameters.player.transform.position, false);
            Guid item = Guid.Empty;
            if (killer != default)
            {
                if (killer.player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                {
                    c.TryUpdateAttackers(killer.playerID.steamID.m_SteamID);
                }
                if (DeathInfo.TryGetValue(parameters.player.channel.owner.playerID.steamID.m_SteamID, out DeathInfo info))
                {
                    UCWarfare.I.GetKillerInfo(out item, out info.distance, out info.killerName, out info.killerTeam, out info.kitName, out info.vehicle, parameters.cause, killer, parameters.player);
                    info.item = item;
                }
                else
                {
                    UCWarfare.I.GetKillerInfo(out item, out float distance, out FPlayerName names, out ulong killerTeam, out string kitname, out ushort turretvehicle, parameters.cause, killer, parameters.player);
                    DeathInfo.Add(parameters.player.channel.owner.playerID.steamID.m_SteamID,
                        new DeathInfo()
                        {
                            distance = distance,
                            item = item,
                            killerName = names,
                            killerTeam = killerTeam,
                            kitName = kitname,
                            vehicle = turretvehicle
                        });
                }
                if (killer.playerID.steamID.m_SteamID != parameters.player.channel.owner.playerID.steamID.m_SteamID) // suicide
                {
                    byte kteam = killer.GetTeamByte();
                    if (kteam != team)
                    {
                        ToastMessage.QueueMessage(killer, new ToastMessage(Translation.Translate("xp_enemy_downed", killer), EToastMessageSeverity.MINI));
                        if (parameters.player.transform.TryGetComponent(out PlaytimeComponent p))
                        {
                            if ((DateTime.Now - p.secondLastAttacker.Value).TotalSeconds < 30 && p.secondLastAttacker.Key != parameters.killer.m_SteamID)
                            {
                                //ToastMessage.QueueMessage(killer, new ToastMessage(Translation.Translate("xp_assist_enemy_downed", killer), EToastMessageSeverity.MINI));
                            }
                        }

                        Stats.StatsManager.ModifyTeam(kteam, t => t.Downs++, false);
                        if (KitManager.HasKit(killer, out Kit kit))
                        {
                            Stats.StatsManager.ModifyStats(killer.playerID.steamID.m_SteamID, s =>
                            {
                                s.Downs++;
                                Stats.WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == kteam);
                                if (kitData == default)
                                {
                                    kitData = new Stats.WarfareStats.KitData() { KitID = kit.Name, Team = kteam, Downs = 1 };
                                    s.Kits.Add(kitData);
                                }
                                else
                                {
                                    kitData.Downs++;
                                }
                            }, false);
                            if (Assets.find(item) is ItemAsset asset && asset != null)
                            {
                                Stats.StatsManager.ModifyWeapon(asset.id, kit.Name, w => w.Downs++, true);
                            }
                        }
                        else
                            Stats.StatsManager.ModifyStats(killer.playerID.steamID.m_SteamID, s => s.Downs++, false);
                    }
                    else
                        ToastMessage.QueueMessage(killer, new ToastMessage("", Translation.Translate("xp_friendly_downed", killer), EToastMessageSeverity.MINI));
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            //L.Log(player.Player.channel.owner.playerID.playerName + " died in ReviveManager.", ConsoleColor.DarkRed);
            SetStanceBetter(player.Player, EPlayerStance.STAND);
            if (DownedPlayers.ContainsKey(player.CSteamID.m_SteamID))
            {
                if (player.Player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer(true);
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
                EffectManager.askEffectClearByID(Squads.SquadManager.config.data.MedicMarker, player.Player.channel.owner.transportConnection);
            }
            ClearInjuredMarker(player.CSteamID.m_SteamID, player.GetTeam());
        }
        private void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            //L.Log(equipment.player.channel.owner.playerID.playerName + " tried to equip", ConsoleColor.DarkRed);
            if (DownedPlayers.ContainsKey(equipment.player.channel.owner.playerID.steamID.m_SteamID))
            {
                shouldAllow = false;
            }
        }
        private void StanceUpdatedLocal(SteamPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (DownedPlayers.ContainsKey(player.playerID.steamID.m_SteamID) && player.player.transform.TryGetComponent(out Reviver reviver))
            {
                reviver.TellStanceNoDelay(EPlayerStance.PRONE);
            }
        }
        public void RegisterMedic(UCPlayer player)
        {
            Medics.Add(player);
            Vector3[] newpositions = GetPositionsOfTeam(player.GetTeam());
            SpawnInjuredMarkers(player.Player.channel.owner.transportConnection, newpositions, true, player.Position);
        }
        public void DeregisterMedic(UCPlayer player)
        {
            Medics.RemoveAll(x => x == null || x.Steam64 == player.Steam64);
            ClearInjuredMarkers(player);
        }
        public void Dispose()
        {
            foreach (DownedPlayerData downPlayer in DownedPlayers.Values.ToList())
            {
                if (downPlayer.parameters.player.transform.TryGetComponent(out Reviver reviver))
                {
                    reviver.FinishKillingPlayer();
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            IEnumerator<UCPlayer> player = Medics
                .Where(x => x.GetTeam() == Team)
                .GetEnumerator();
            while (player.MoveNext())
            {
                var sqrDistance = (player.Current.Position - Position).sqrMagnitude;
                var sqrmLimit = Math.Pow(Squads.SquadManager.config.data.MedicRange, 2);
                if (sqrDistance >= 1 && sqrDistance <= sqrmLimit)
                    EffectManager.sendEffectReliable(Squads.SquadManager.config.data.InjuredMarker, player.Current.Player.channel.owner.transportConnection, Position);
            }
            player.Dispose();
        }
        internal void GiveUp(Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (DownedPlayers.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out DownedPlayerData p))
            {
                player.life.askDamage(byte.MaxValue, Vector3.down, p.parameters.cause, p.parameters.limb, p.parameters.killer, out _, p.parameters.trackKill, p.parameters.ragdollEffect, false, true);
                // player and Revive UI will be removed from list in OnDeath
            }
        }
        public void SpawnInjuredMarkers(IEnumerator<UCPlayer> players, Vector3[] positions, bool dispose, bool clearAll)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            while (players.MoveNext())
            {
                if (clearAll)
                    EffectManager.askEffectClearByID(Squads.SquadManager.config.data.InjuredMarker, players.Current.Player.channel.owner.transportConnection);
                for (int i = 0; i < positions.Length; i++)
                {
                    var sqrDistance = (players.Current.Position - positions[i]).sqrMagnitude;
                    var sqrmLimit = Math.Pow(Squads.SquadManager.config.data.MedicRange, 2);
                    if (sqrDistance >= 1 && sqrDistance <= sqrmLimit)
                        EffectManager.sendEffectReliable(Squads.SquadManager.config.data.InjuredMarker, players.Current.Player.channel.owner.transportConnection, positions[i]);
                }
            }
            if (dispose) players.Dispose();
        }
        public void SpawnMedicMarkers(IEnumerator<ITransportConnection> players, Vector3[] positions, bool dispose, bool clearAll)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            while (players.MoveNext())
            {
                if (clearAll)
                    EffectManager.askEffectClearByID(Squads.SquadManager.config.data.MedicMarker, players.Current);
                for (int i = 0; i < positions.Length; i++)
                    EffectManager.sendEffectReliable(Squads.SquadManager.config.data.MedicMarker, players.Current, positions[i]);
            }
            if (dispose) players.Dispose();
        }
        public void SpawnInjuredMarkers(ITransportConnection player, Vector3[] positions, bool clearAll, Vector3 center)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (clearAll)
                EffectManager.askEffectClearByID(Squads.SquadManager.config.data.InjuredMarker, player);
            for (int i = 0; i < positions.Length; i++)
                if ((center - positions[i]).sqrMagnitude <= Math.Pow(Squads.SquadManager.config.data.MedicRange, 2))
                    EffectManager.sendEffectReliable(Squads.SquadManager.config.data.InjuredMarker, player, positions[i]);
        }
        public void SpawnMedicMarkers(ITransportConnection player, Vector3[] positions, bool clearAll)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (clearAll)
                EffectManager.askEffectClearByID(Squads.SquadManager.config.data.MedicMarker, player);
            for (int i = 0; i < positions.Length; i++)
                EffectManager.sendEffectReliable(Squads.SquadManager.config.data.MedicMarker, player, positions[i]);
        }
        public void ClearInjuredMarker(ulong clearedPlayer, ulong Team)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            IEnumerator<UCPlayer> medics = Medics
                .Where(x => x.GetTeam() == Team)
                .GetEnumerator();
            ulong[] downed = DownedPlayers.Keys.ToArray();
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < downed.Length; i++)
            {
                if (downed[i] == clearedPlayer) continue;
                UCPlayer? player = UCPlayer.FromID(downed[i]);
                if (player == null) continue;
                positions.Add(player.Position);
            }
            Vector3[] newpositions = positions.ToArray();
            SpawnInjuredMarkers(medics, newpositions, true, true);
        }
        public void ClearInjuredMarkers(UCPlayer medic)
        {
            EffectManager.askEffectClearByID(Squads.SquadManager.config.data.InjuredMarker, medic.Player.channel.owner.transportConnection);
        }
        public Vector3[] GetPositionsOfTeam(ulong Team)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ulong[] downed = DownedPlayers.Keys.ToArray();
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < downed.Length; i++)
            {
                UCPlayer? player = UCPlayer.FromID(downed[i]);
                if (player == null || player.GetTeam() != Team) continue;
                positions.Add(player.Position);
            }
            return positions.ToArray();
        }
        public void UpdateInjuredMarkers()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            IEnumerator<UCPlayer> medics = Medics.
                Where(x => x.GetTeam() == 1)
                .GetEnumerator();
            Vector3[] newpositions = GetPositionsOfTeam(1);
            SpawnInjuredMarkers(medics, newpositions, true, true);
            medics = Medics
                .Where(x => x.GetTeam() == 2)
                .GetEnumerator();
            newpositions = GetPositionsOfTeam(2);
            SpawnInjuredMarkers(medics, newpositions, true, true);
        }
        public void UpdateMedicMarkers()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < DownedPlayers.Keys.Count; i++)
            {
                UCPlayer? downed = UCPlayer.FromID(DownedPlayers.Keys.ElementAt(i));
                if (downed == null) continue;
                ulong team = downed.GetTeam();
                Vector3[] medics = Medics
                    .Where(x => x.GetTeam() == team &&
                        (x.Position - downed.Position).sqrMagnitude <
                        Math.Pow(Squads.SquadManager.config.data.MedicRange, 2) &&
                        x.connection != downed.connection)
                    .Select(x => x.Position)
                    .ToArray();
                SpawnMedicMarkers(downed.Player.channel.owner.transportConnection, medics, true);
            }
        }
        public void UpdateMedicMarkers(ITransportConnection player, ulong team, Vector3 origin, bool clearOld)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (team > 0 && team < 3) return;
            Vector3[] medics = Medics
                .Where(x => x.GetTeam() == team &&
                    (x.Position - origin).sqrMagnitude <
                    Math.Pow(Squads.SquadManager.config.data.MedicRange, 2) &&
                        x.connection != player)
                .Select(x => x.Position)
                .ToArray();
            SpawnMedicMarkers(player, medics, clearOld);
        }
        private class Reviver : UnturnedPlayerComponent
        {
            private Coroutine? stance;
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (killerid.TryGetPlaytimeComponent(out Components.PlaytimeComponent killer) && killer.stats != null && killer.stats is IPVPModeStats pvp)
                {
                    pvp.AddDamage(damage);
                }
                if (player.TryGetPlaytimeComponent(out Components.PlaytimeComponent victim))
                {
                    victim.TryUpdateAttackers(killerid.m_SteamID);
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
                //L.Log("Checked stance of " + Player.Player.channel.owner.playerID.playerName + " to " + stance.ToString() + ".", ConsoleColor.DarkRed);
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
            public void RevivePlayer(IRevives? g = default, bool remove = true)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (g == default) Data.Is<IRevives>(out g);
                if (g != default)
                {
                    Player.Player.movement.sendPluginSpeedMultiplier(1.0f);
                    Player.Player.movement.sendPluginJumpMultiplier(1.0f);
                    Player.Player.life.serverSetBleeding(false);
                    CancelStance();
                    if (remove)
                    {
                        g.ReviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                        g.ReviveManager.DeathInfo.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                    }
                }
            }
            public void FinishKillingPlayer(bool isDead = false)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (Data.Is(out IRevives g))
                {
                    this.RevivePlayer(g, false);
                    if (!isDead)
                    {
                        DamagePlayerParameters parameters = g.ReviveManager.DownedPlayers[Player.Player.channel.owner.playerID.steamID.m_SteamID].parameters;
                        parameters.damage = 100.0f;
                        parameters.respectArmor = false;
                        parameters.applyGlobalArmorMultiplier = false;
                        DamageTool.damagePlayer(parameters, out _);
                    }
                    g.ReviveManager.DownedPlayers.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                    g.ReviveManager.DeathInfo.Remove(Player.Player.channel.owner.playerID.steamID.m_SteamID);
                }
            }
        }
        public struct DownedPlayerData
        {
            public DamagePlayerParameters parameters;
            public DateTime start;

            public DownedPlayerData(DamagePlayerParameters parameters)
            {
                this.parameters = parameters;
                start = DateTime.Now;
            }
        }
    }
}
