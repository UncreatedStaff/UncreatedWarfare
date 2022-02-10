using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets
{
    public class TicketManager : IDisposable
    {
        public static Config<TicketData> config = new Config<TicketData>(Data.TicketStorage, "config.json");

        public static int Team1Tickets;
        public static int Team2Tickets;
        public static DateTime TimeSinceMatchStart;
        public TicketManager()
        {
            TimeSinceMatchStart = DateTime.Now;

            Team1Tickets = Gamemode.Config.TeamCTF.StartingTickets;
            Team2Tickets = Gamemode.Config.TeamCTF.StartingTickets;

            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }
        public static void OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerDeathOffline(ulong deadteam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (deadteam == 1)
                AddTeam1Tickets(-1);
            else if (deadteam == 2)
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerSuicide(UCWarfare.SuicideEventArgs eventArgs)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);
        }
        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out Insurgency insurgency))
            {
                if (parameters.dead.quests.groupID.m_SteamID == insurgency.DefendingTeam)
                {
                    insurgency.AddIntelligencePoints(1);
                    if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is InsurgencyPlayerStats s)
                        s._intelligencePointsCollected++;
                    insurgency.GameStats.intelligenceGathered++;
                }
            }

            Points.AwardXP(
                parameters.killer,
                Points.XPConfig.EnemyKilledXP,
                Translation.Translate("xp_enemy_killed", parameters.killer));

            if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent component))
            {
                ulong killerID = parameters.killer.channel.owner.playerID.steamID.m_SteamID;
                ulong victimID = parameters.dead.channel.owner.playerID.steamID.m_SteamID;

                UCPlayer assister = UCPlayer.FromID(component.secondLastAttacker.Key);
                if (assister != null && assister.Steam64 != killerID && assister.Steam64 != victimID && (DateTime.Now - component.secondLastAttacker.Value).TotalSeconds <= 30)
                {
                    Points.AwardXP(
                        assister,
                        Points.XPConfig.KillAssistXP,
                        Translation.Translate("xp_kill_assist", parameters.killer));
                }
                component.ResetAttackers();
            }

            Points.TryAwardDriverAssist(parameters.killer, Points.XPConfig.EnemyKilledXP, 1);
        }
        public static void OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Points.AwardXP(
                parameters.killer,
                Points.XPConfig.FriendlyKilledXP,
                Translation.Translate("xp_friendly_killed", parameters.killer));
        }
        private static void OnVehicleExploded(InteractableVehicle vehicle)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ulong lteam = vehicle.lockedGroup.m_SteamID.GetTeam();

                if (lteam == 1)
                    AddTeam1Tickets(-1 * data.TicketCost);
                else if (lteam == 2)
                    AddTeam2Tickets(-1 * data.TicketCost);

                if (vehicle.transform.gameObject.TryGetComponent(out VehicleComponent vc))
                {
                    UCPlayer owner = UCPlayer.FromID(vehicle.lockedOwner.m_SteamID);
                    if (Points.XPConfig.VehicleDestroyedXP.ContainsKey(data.Type))
                    {
                        UCPlayer player = UCPlayer.FromID(vc.lastDamager);
                        bool wasCrashed = false;

                        if (player == null)
                            player = UCPlayer.FromID(vc.LastDriver);
                        if (player == null)
                            return;
                        else if (player.GetTeam() == vehicle.lockedGroup.m_SteamID && vc.lastDamageOrigin == EDamageOrigin.Vehicle_Collision_Self_Damage)
                            wasCrashed = true;

                        ulong dteam = player.GetTeam();
                        bool vehicleWasEnemy = (dteam == 1 && lteam == 2) || (dteam == 2 && lteam == 1);
                        bool vehicleWasFriendly = dteam == lteam;
                        if (!vehicleWasFriendly)
                            Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);
                        if (!Points.XPConfig.VehicleDestroyedXP.TryGetValue(data.Type, out int fullXP))
                            fullXP = 0;
                        string message = string.Empty;

                        switch (data.Type)
                        {
                            case EVehicleType.HUMVEE:
                                message = "humvee_destroyed";
                                break;
                            case EVehicleType.TRANSPORT:
                                message = "transport_destroyed";
                                break;
                            case EVehicleType.LOGISTICS:
                                message = "logistics_destroyed";
                                break;
                            case EVehicleType.SCOUT_CAR:
                                message = "scoutcar_destroyed";
                                break;
                            case EVehicleType.APC:
                                message = "apc_destroyed";
                                break;
                            case EVehicleType.IFV:
                                message = "ifv_destroyed";
                                break;
                            case EVehicleType.MBT:
                                message = "tank_destroyed";
                                break;
                            case EVehicleType.HELI_TRANSPORT:
                                message = "transheli_destroyed";
                                break;
                            case EVehicleType.HELI_ATTACK:
                                message = "attackheli_destroyed";
                                break;
                            case EVehicleType.JET:
                                message = "jet_destroyed";
                                break;
                            case EVehicleType.EMPLACEMENT:
                                message = "emplacement_destroyed";
                                break;
                        }


                        float totalDamage = 0;
                        foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in vc.DamageTable)
                        {
                            if ((DateTime.Now - entry.Value.Value).TotalSeconds < 60)
                                totalDamage += entry.Value.Key;
                        }

                        if (vehicleWasEnemy)
                        {
                            Asset asset = Assets.find(vc.item);
                            string reason = "";
                            if (asset != null)
                            {
                                if (asset is ItemAsset item)
                                    reason = item.itemName;
                                else if (asset is VehicleAsset v)
                                    reason = "suicide " + v.vehicleName;
                            }

                            if (reason == "")
                                Chat.Broadcast("VEHICLE_DESTROYED_UNKNOWN", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName);
                            else
                                Chat.Broadcast("VEHICLE_DESTROYED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName, reason);

                            QuestManager.OnVehicleDestroyed(owner, player, data, vc);

                            float resMax = 0f;
                            UCPlayer resMaxPl = null;
                            foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in vc.DamageTable)
                            {
                                if ((DateTime.Now - entry.Value.Value).TotalSeconds < 60)
                                {
                                    float responsibleness = entry.Value.Key / totalDamage;
                                    int reward = Mathf.RoundToInt(responsibleness * fullXP);

                                    UCPlayer attacker = UCPlayer.FromID(entry.Key);
                                    if (attacker != null && attacker.GetTeam() != vehicle.lockedGroup.m_SteamID)
                                    {
                                        if (attacker.CSteamID.m_SteamID == vc.lastDamager)
                                        {
                                            Points.AwardXP(attacker, reward, Translation.Translate("xp_" + message, player));
                                            Points.TryAwardDriverAssist(player.Player, fullXP, data.TicketCost);
                                        }
                                        else if (responsibleness > 0.1F)
                                            Points.AwardXP(attacker, reward, Translation.Translate("xp_vehicle_assist", attacker));
                                        if (responsibleness > resMax)
                                        {
                                            resMax = responsibleness;
                                            resMaxPl = attacker;
                                        }
                                    }
                                }
                            }

                            if (resMax > 0 && player.Steam64 != resMaxPl?.Steam64)
                            {
                                QuestManager.OnVehicleDestroyed(owner, resMaxPl, data, vc);
                            }
                            
                            Stats.StatsManager.ModifyStats(player.Steam64, s => s.VehiclesDestroyed++, false);
                            Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesDestroyed++);
                        }
                        else if (vehicleWasFriendly)
                        {
                            Chat.Broadcast("VEHICLE_TEAMKILLED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), "", vehicle.asset.vehicleName);

                            if (message != string.Empty) message = "xp_friendly_" + message;
                            Points.AwardXP(player.Player, -fullXP, Translation.Translate(message, player.Steam64));
                            Invocations.Warfare.LogFriendlyVehicleKill.NetInvoke(player.Steam64, vehicle.id, vehicle.asset.vehicleName ?? vehicle.id.ToString(), DateTime.Now);
                        }

                        float missingQuota = vc.Quota - vc.RequiredQuota;
                        if (missingQuota < 0)
                        {
                            // give quota penalty
                            if (vc.RequiredQuota != -1 && (vehicleWasEnemy || wasCrashed))
                            {
                                for (byte i = 0; i < vehicle.passengers.Length; i++)
                                {
                                    Passenger passenger = vehicle.passengers[i];

                                    if (passenger.player is not null)
                                    {
                                        vc.EvaluateUsage(passenger.player);
                                    }
                                }

                                double totalTime = 0;
                                foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                                    totalTime += entry.Value;

                                foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                                {
                                    float responsibleness = (float)(entry.Value / totalTime);
                                    int penalty = Mathf.RoundToInt(responsibleness * missingQuota * 60F);

                                    var assetWaster = UCPlayer.FromID(entry.Key);
                                    if (assetWaster != null)
                                        Points.AwardXP(assetWaster, penalty, Translation.Translate("xp_wasting_assets", assetWaster));
                                }
                            }
                        }

                        if (vehicle.TryGetComponent(out SpawnedVehicleComponent svc))
                            Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, svc.spawn.SpawnPadInstanceID, vc.lastDamager, vehicle.asset.GUID, vc.item, vc.lastDamageOrigin, vehicleWasFriendly);
                        else
                            Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, uint.MaxValue, vc.lastDamager, vehicle.asset.GUID, vc.item, vc.lastDamageOrigin, vehicleWasFriendly);
                    }
                }
            }
        }
        public static void OnRoundWin(ulong team)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            float winMultiplier = 0.15f;

            List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UCPlayer player = players[i];

                if (player.CSteamID.TryGetPlaytimeComponent(out PlaytimeComponent component) && component.stats is IExperienceStats exp)
                {
                    //if (exp.XPGained > 0)
                    //    Points.AwardXP(player.Player, Mathf.RoundToInt(exp.XPGained * winMultiplier), Translation.Translate("xp_victory", player.Steam64));

                    if (exp.OFPGained > 0)
                        Points.AwardTW(player, Mathf.RoundToInt(exp.OFPGained * winMultiplier), Translation.Translate("xp_victory", player.Steam64));
                }
            }
        }
        public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            int team1bleed = GetTeamBleed(1);
            int team2bleed = GetTeamBleed(2);

            if (Data.Is<Invasion>(out _))
            {
                if (capturedTeam == 1)
                {
                    Team1Tickets += Gamemode.Config.Invasion.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2)
                {
                    Team2Tickets += Gamemode.Config.Invasion.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }
            }
            else if (Data.Is(out IFlagRotation r))
            {
                if (capturedTeam == 1) flag.HasBeenCapturedT1 = true;
                if (capturedTeam == 2) flag.HasBeenCapturedT2 = true;

                if (r.Rotation.Count / 2f + 0.5f == flag.index) // if is middle flag
                {
                    if (capturedTeam == 1) Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                    if (capturedTeam == 2) Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                }

                if (team2bleed < 0)
                {
                    Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                }
                else if (team1bleed < 0)
                {
                    Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                }

                if (lostTeam == 1) Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
                if (lostTeam == 2) Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
            }
            

            UpdateUITeam1(team1bleed);
            UpdateUITeam2(team2bleed);

            Dictionary<Squad, int> alreadyUpdated = new Dictionary<Squad, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = Points.XPConfig.FlagCapturedXP;

                Points.AwardXP(player, player.NearbyMemberBonus(xp, 60), Translation.Translate("xp_flag_captured", player.Steam64));

                if (player.IsNearSquadLeader(50))
                {
                    if (alreadyUpdated.TryGetValue(player.Squad, out int amount))
                    {
                        amount += Points.TWConfig.MemberFlagCapturePoints;
                    }
                    else
                    {
                        alreadyUpdated.Add(player.Squad, Points.TWConfig.MemberFlagCapturePoints);
                    }
                }
            }

            for (int i = 0; i < SquadManager.Squads.Count; i++)
            {
                if (alreadyUpdated.TryGetValue(SquadManager.Squads[i], out int amount))
                {
                    Points.AwardTW(SquadManager.Squads[i].Leader.Player, amount, "");
                }
            }
        }
        public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = Points.XPConfig.FlagNeutralizedXP;

                Points.AwardXP(player, xp, Translation.Translate("xp_flag_neutralized", player.Steam64));
                Points.AwardXP(player, player.NearbyMemberBonus(xp, 60) - xp, Translation.Translate("xp_squad_bonus", player.Steam64));
            }

            UpdateUITeam1(GetTeamBleed(1));
            UpdateUITeam2(GetTeamBleed(2));
        }
        public static void OnFlag10Seconds()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out IFlagRotation fg))
            {
                for (int i = 0; i < fg.Rotation.Count; i++)
                {
                    Flag flag = fg.Rotation[i];
                    if (flag.LastDeltaPoints > 0 && flag.Owner != 1)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                            Points.AwardXP(flag.PlayersOnFlagTeam1[j],
                                Points.XPConfig.FlagAttackXP,
                                Translation.Translate("xp_flag_attack", flag.PlayersOnFlagTeam1[j]));
                    }
                    else if (flag.LastDeltaPoints < 0 && flag.Owner != 2)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                            Points.AwardXP(flag.PlayersOnFlagTeam2[j],
                                Points.XPConfig.FlagAttackXP,
                                Translation.Translate("xp_flag_attack", flag.PlayersOnFlagTeam2[j]));
                    }
                    else if (flag.Owner == 1 && flag.IsObj(2) && flag.Team2TotalCappers == 0)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                            Points.AwardXP(flag.PlayersOnFlagTeam1[j],
                                Points.XPConfig.FlagDefendXP,
                                Translation.Translate("xp_flag_defend", flag.PlayersOnFlagTeam1[j]));
                    }
                    else if (flag.Owner == 2 && flag.IsObj(1) && flag.Team1TotalCappers == 0)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                            Points.AwardXP(flag.PlayersOnFlagTeam2[j],
                                Points.XPConfig.FlagDefendXP,
                                Translation.Translate("xp_flag_defend", flag.PlayersOnFlagTeam2[j]));
                    }
                }
            }
        }
        public static void OnCache10Seconds()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out Insurgency ins))
            {
                foreach (Insurgency.CacheData cache in ins.ActiveCaches)
                {
                    if (cache.IsActive && !cache.IsDestroyed)
                    {
                        for (int j = 0; j < cache.Cache.NearbyDefenders.Count; j++)
                            Points.AwardXP(cache.Cache.NearbyDefenders[j],
                                Points.XPConfig.FlagDefendXP,
                                Translation.Translate("xp_flag_defend", cache.Cache.NearbyDefenders[j]));
                    }
                }
            }
        }
        public static void OnPlayerJoined(UCPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            ulong team = player.GetTeam();
            int bleed = GetTeamBleed(player.GetTeam());
            GetUIDisplayerInfo(player.GetTeam(), bleed, out ushort UIID, out string tickets, out string message);
            UpdateUI(player.connection, UIID, tickets, message);
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            EffectManager.askEffectClearByID(config.data.Team1TicketUIID, player.transportConnection);
            EffectManager.askEffectClearByID(config.data.Team2TicketUIID, player.transportConnection);
            int bleed = GetTeamBleed(player.GetTeam());
            GetUIDisplayerInfo(player.GetTeam(), bleed, out ushort UIID, out string tickets, out string message);
            UpdateUI(player.transportConnection, UIID, tickets, message);
        }
        public static void OnStagingPhaseEnded()
        {
            TimeSinceMatchStart = DateTime.Now;
        }
        public static void OnNewGameStarting()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out Invasion invasion))
            {
                int attack = Gamemode.Config.Invasion.AttackStartingTickets;
                int defence = Gamemode.Config.Invasion.AttackStartingTickets + (invasion.Rotation.Count * Gamemode.Config.Invasion.TicketsFlagCaptured);

                if (invasion.AttackingTeam == 1)
                {
                    Team1Tickets = attack;
                    Team2Tickets = defence;
                }
                else if (invasion.AttackingTeam == 2)
                {
                    Team2Tickets = attack;
                    Team1Tickets = defence;
                }
            }
            else if (Data.Is(out Insurgency insurgency))
            {
                int attack = Gamemode.Config.Insurgency.AttackStartingTickets;
                int defence = insurgency.CachesLeft;

                if (insurgency.AttackingTeam == 1)
                {
                    Team1Tickets = attack;
                    Team2Tickets = defence;
                }
                else if (insurgency.AttackingTeam == 2)
                {
                    Team2Tickets = attack;
                    Team1Tickets = defence;
                }
            }
            else
            {
                Team1Tickets = Gamemode.Config.TeamCTF.StartingTickets;
                Team2Tickets = Gamemode.Config.TeamCTF.StartingTickets;
            }

            UpdateUITeam1(GetTeamBleed(1));
            UpdateUITeam2(GetTeamBleed(2));
        }

        public static void AddTeam1Tickets(int number)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 1)
                return;

            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                Data.Gamemode.DeclareWin(2);
            }
            UpdateUITeam1(GetTeamBleed(1));
        }
        public static void AddTeam2Tickets(int number)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 2)
                return;

            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                Data.Gamemode.DeclareWin(1);
            }
            UpdateUITeam2(GetTeamBleed(2));
        }
        public static void GetUIDisplayerInfo(ulong team, int bleed, out ushort UIID, out string tickets, out string message)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            UIID = 0;
            tickets = "";
            message = "";

            if (TeamManager.IsTeam1(team))
            {
                tickets = Team1Tickets.ToString();
                UIID = config.data.Team1TicketUIID;
            }

            else if (TeamManager.IsTeam2(team))
            {
                tickets = Team2Tickets.ToString();
                UIID = config.data.Team2TicketUIID;
            }

            if (Data.Is(out Insurgency insurgency))
            {
                if (insurgency.DefendingTeam == team)
                {
                    tickets = insurgency.CachesLeft + " left";
                    message = "DEFEND THE WEAPONS CACHES";
                }
                else if (insurgency.AttackingTeam == team)
                {
                    if (insurgency.DiscoveredCaches.Count == 0)
                        message = "FIND THE WEAPONS CACHES\n(kill enemies for intel)";
                    else
                        message = "DESTROY THE WEAPONS CACHES";
                }
                else
                    message = "";
            }
            else if (bleed < 0)
            {
                message = $"{bleed} per minute".Colorize("eb9898");
            }
        }
        public static void UpdateUI(ITransportConnection connection, ushort UIID, string tickets, string message)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                tickets.ToString(Data.Locale),
                string.Empty,
                message
                );

        }
        public static void UpdateUITeam1(int bleed = 0)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            GetUIDisplayerInfo(1, bleed, out ushort UIID, out string tickets, out string message);

            List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].connection, UIID, tickets, message);
            }
        }
        public static void UpdateUITeam2(int bleed = 0)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            GetUIDisplayerInfo(2, bleed, out ushort UIID, out string tickets, out string message);

            List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].connection, UIID, tickets, message);
            }
        }
        public static int GetTeamBleed(ulong team)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (Data.Is(out IFlagRotation fg))
            {
                if (Data.Is(out Invasion invasion) && team == invasion.AttackingTeam)
                {
                    int defenderFlags = fg.Rotation.Where(f => f.Owner == invasion.DefendingTeam).Count();

                    if (defenderFlags == fg.Rotation.Count)
                    {
                        return -1;
                    }
                }
                else
                {
                    int friendly = fg.Rotation.Where(f => f.Owner == team).Count();
                    int enemy = fg.Rotation.Where(f => f.Owner != team && f.Owner != 0).Count();
                    int total = fg.Rotation.Count;

                    float friendlyRatio = (float)friendly / total;
                    float enemyRatio = (float)enemy / total;

                    if (enemyRatio >= 0.6f)
                    {
                        if (enemyRatio > 0.75f)
                        {
                            if (enemyRatio > 0.85f)
                                return -3;
                            else
                                return -2;
                        }
                        else
                            return -1;
                    }
                }
            }
            return 0;
        }
        //public static void AwardSquadXP(UCPlayer ucplayer, float range, int xp, int ofp, string KeyplayerTranslationKey, string squadTranslationKey, float squadMultiplier)
        //{
        //    string xpstr = Translation.Translate(KeyplayerTranslationKey, ucplayer.Steam64);
        //    string sqstr = Translation.Translate(squadTranslationKey, ucplayer.Steam64);
        //    Points.AwardXP(ucplayer.Player, xp, xpstr);

        //    if (ucplayer.Squad != null && ucplayer.Squad?.Members.Count > 1)
        //    {
        //        if (ucplayer == ucplayer.Squad.Leader)
        //            OfficerManager.AddOfficerPoints(ucplayer.Player, ofp, sqstr);

        //        int squadxp = (int)Math.Round(xp * squadMultiplier);
        //        int squadofp = (int)Math.Round(ofp * squadMultiplier);

        //        if (squadxp > 0)
        //        {
        //            for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
        //            {
        //                UCPlayer member = ucplayer.Squad.Members[i];
        //                if (member != ucplayer && ucplayer.IsNearOtherPlayer(member, range))
        //                {
        //                    Points.AwardXP(member.Player, squadxp, sqstr);
        //                    if (member.IsSquadLeader())
        //                        OfficerManager.AddOfficerPoints(ucplayer.Player, squadofp, sqstr);
        //                }
        //            }
        //        }
        //    }
        //}
        public void Dispose()
        {
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
        }
    }
    public class TicketData : ConfigData
    {
        public int TicketHandicapDifference;
        public int FOBCost;
        public ushort Team1TicketUIID;
        public ushort Team2TicketUIID;

        public override void SetDefaults()
        {
            TicketHandicapDifference = 40;
            FOBCost = 15;
            Team1TicketUIID = 36035;
            Team2TicketUIID = 36058;
        }
        public TicketData() { }
    }
}
