using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets;

public class TicketManager : BaseSingleton, IPlayerInitListener, IGameStartListener
{
    private static TicketManager Singleton;
    public static bool Loaded => Singleton.IsLoaded();
    public static Config<TicketData> config = new Config<TicketData>(Data.TicketStorage, "config.json");

    public static int Team1Tickets;
    public static int Team2Tickets;
    public TicketManager()
    {
    }
    public override void Load()
    {
        Singleton = this;
        Team1Tickets = Gamemode.Config.TeamCTF.StartingTickets;
        Team2Tickets = Gamemode.Config.TeamCTF.StartingTickets;
        VehicleManager.OnVehicleExploded += OnVehicleExploded;
        EventDispatcher.OnPlayerDied += OnPlayerDeath;
    }
    public override void Unload()
    {
        Singleton = null!;
        VehicleManager.OnVehicleExploded -= OnVehicleExploded;
    }
    void IPlayerInitListener.OnPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        int bleed = GetTeamBleed(player.GetTeam());
        GetUIDisplayerInfo(player.GetTeam(), bleed, out ushort UIID, out string tickets, out string message);
        UpdateUI(player.Connection, UIID, tickets, message);
    }
    private void OnPlayerDeath(PlayerDied e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.DeadTeam == 1)
            AddTeam1Tickets(-1);
        else if (e.DeadTeam == 2)
            AddTeam2Tickets(-1);
        if (e.Killer is not null)
        {
            if (!e.WasTeamkill)
                OnEnemyKilled(e.Player, e.Killer);
            else
                OnFriendlyKilled(e.Player, e.Killer);
        }
    }
    public static void OnEnemyKilled(UCPlayer dead, UCPlayer killer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out Insurgency insurgency))
        {
            if (dead.GetTeam() == insurgency.DefendingTeam)
            {
                insurgency.AddIntelligencePoints(1);
                if (killer.Player.TryGetPlayerData(out UCPlayerData c) && c.stats is InsurgencyPlayerStats s)
                    s._intelligencePointsCollected++;
                insurgency.GameStats.intelligenceGathered++;
            }
        }

        Points.AwardXP(killer, Points.XPConfig.EnemyKilledXP, Translation.Translate("xp_enemy_killed", killer));

        if (dead.Player.TryGetPlayerData(out UCPlayerData component))
        {
            ulong killerID = killer.Steam64;
            ulong victimID = dead.Steam64;

                UCPlayer? assister = UCPlayer.FromID(component.secondLastAttacker.Key);
                if (assister != null && assister.Steam64 != killerID && assister.Steam64 != victimID && (DateTime.Now - component.secondLastAttacker.Value).TotalSeconds <= 30)
                {
                    Points.AwardXP(
                        assister,
                        Points.XPConfig.KillAssistXP,
                        Translation.Translate("xp_kill_assist", killer));
                }

                if (dead.Player.TryGetComponent(out SpottedComponent spotted))
                {
                    spotted.OnTargetKilled(Points.XPConfig.EnemyKilledXP);
                }

                component.ResetAttackers();
            }

        Points.TryAwardDriverAssist(killer, Points.XPConfig.EnemyKilledXP, 1);
    }
    public static void OnFriendlyKilled(UCPlayer dead, UCPlayer killer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Points.AwardXP(killer, Points.XPConfig.FriendlyKilledXP, Translation.Translate("xp_friendly_killed", killer));
    }
    private static void OnVehicleExploded(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

            var spotted = vehicle.transform.GetComponent<SpottedComponent>();

            if (vehicle.gameObject.TryGetComponent(out FOBs.BuiltBuildableComponent comp))
                UnityEngine.Object.Destroy(comp);

            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ulong lteam = vehicle.lockedGroup.m_SteamID.GetTeam();

                if (lteam == 1)
                    AddTeam1Tickets(-data.TicketCost);
                else if (lteam == 2)
                    AddTeam2Tickets(-data.TicketCost);

                if (vehicle.transform.gameObject.TryGetComponent(out VehicleComponent vc))
                {
                    UCPlayer? owner = UCPlayer.FromID(vehicle.lockedOwner.m_SteamID);
                    if (Points.XPConfig.VehicleDestroyedXP.ContainsKey(data.Type))
                    {
                        UCPlayer? player = UCPlayer.FromID(vc.LastInstigator);

                        if (player == null)
                            player = UCPlayer.FromID(vc.LastDriver);
                        if (player == null)
                            return;

                        ulong dteam = player.GetTeam();
                        bool vehicleWasEnemy = (dteam == 1 && lteam == 2) || (dteam == 2 && lteam == 1);
                        bool vehicleWasFriendly = dteam == lteam;
                        if (!vehicleWasFriendly)
                            Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);
                        if (!Points.XPConfig.VehicleDestroyedXP.TryGetValue(data.Type, out int fullXP))
                            fullXP = 0;
                        string message = string.Empty;

                        if (vc.IsAircraft)
                            message = "xp_vehicle_destroyed";
                        else
                            message = "xp_aircraft_destroyed";


                        float totalDamage = 0;
                        foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in vc.DamageTable)
                        {
                            if ((DateTime.Now - entry.Value.Value).TotalSeconds < 60)
                                totalDamage += entry.Value.Key;
                        }

                        if (vehicleWasEnemy)
                        {
                            Asset asset = Assets.find(vc.LastItem);
                            string reason = string.Empty;
                            if (asset != null)
                            {
                                if (asset is ItemAsset item)
                                    reason = item.itemName;
                                else if (asset is VehicleAsset v)
                                    reason = "suicide " + v.vehicleName;
                            }

                                int distance = Mathf.RoundToInt((player.Position - vehicle.transform.position).magnitude);

                                if (reason == "")
                                    Chat.Broadcast("VEHICLE_DESTROYED_UNKNOWN", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName);
                                else
                                    Chat.Broadcast("VEHICLE_DESTROYED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName, reason, distance.ToString());

                            ActionLog.Add(EActionLogType.OWNED_VEHICLE_DIED, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} ID: {vehicle.instanceID}" +
                                                                             $" - Destroyed by {player.Steam64.ToString(Data.Locale)}", vehicle.lockedOwner.m_SteamID);

                            QuestManager.OnVehicleDestroyed(owner, player, data, vc);

                            float resMax = 0f;
                            UCPlayer? resMaxPl = null;
                            foreach (KeyValuePair<ulong, KeyValuePair<ushort, DateTime>> entry in vc.DamageTable)
                            {
                                if ((DateTime.Now - entry.Value.Value).TotalSeconds < 60)
                                {
                                    float responsibleness = entry.Value.Key / totalDamage;
                                    int reward = Mathf.RoundToInt(responsibleness * fullXP);

                                        UCPlayer? attacker = UCPlayer.FromID(entry.Key);
                                        if (attacker != null && attacker.GetTeam() != vehicle.lockedGroup.m_SteamID)
                                        {
                                            if (attacker.CSteamID.m_SteamID == vc.LastInstigator)
                                            {
                                                Points.AwardXP(attacker, reward, Translation.Translate(message, player, data.Type.ToString()).ToUpper());
                                                Points.TryAwardDriverAssist(player.Player, fullXP, data.TicketCost);

                                                if (spotted != null)
                                                {
                                                    spotted.OnTargetKilled(reward);
                                                    UnityEngine.Object.Destroy(spotted);
                                                }
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

                            if (resMaxPl != null && resMax > 0 && player.Steam64 != resMaxPl.Steam64)
                            {
                                QuestManager.OnVehicleDestroyed(owner, resMaxPl, data, vc);
                            }
                        
                            Stats.StatsManager.ModifyStats(player.Steam64, s => s.VehiclesDestroyed++, false);
                            Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesDestroyed++);
                        }
                        else if (vehicleWasFriendly)
                        {
                            Chat.Broadcast("VEHICLE_TEAMKILLED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), "", vehicle.asset.vehicleName);

                            ActionLog.Add(EActionLogType.OWNED_VEHICLE_DIED, $"{vehicle.asset.vehicleName} / {vehicle.id} / {vehicle.asset.GUID:N} ID: {vehicle.instanceID}" +
                                                                             $" - Destroyed by {player.Steam64.ToString(Data.Locale)}", vehicle.lockedOwner.m_SteamID);

                            if (message != string.Empty) message = "xp_friendly_" + message;
                            Points.AwardCredits(player, Mathf.Clamp(data.CreditCost, 5, 1000), Translation.Translate(message, player.Steam64), true);
                            OffenseManager.NetCalls.SendVehicleTeamkilled.NetInvoke(player.Steam64, vehicle.id, vehicle.asset.vehicleName ?? vehicle.id.ToString(), DateTime.Now);
                        }

                        //float missingQuota = vc.Quota - vc.RequiredQuota;
                        //if (missingQuota < 0)
                        //{
                        //    // give quota penalty
                        //    if (vc.RequiredQuota != -1 && (vehicleWasEnemy || wasCrashed))
                        //    {
                        //        for (byte i = 0; i < vehicle.passengers.Length; i++)
                        //        {
                        //            Passenger passenger = vehicle.passengers[i];

                        //            if (passenger.player is not null)
                        //            {
                        //                vc.EvaluateUsage(passenger.player);
                        //            }
                        //        }

                        //        double totalTime = 0;
                        //        foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                        //            totalTime += entry.Value;

                        //        foreach (KeyValuePair<ulong, double> entry in vc.UsageTable)
                        //        {
                        //            float responsibleness = (float)(entry.Value / totalTime);
                        //            int penalty = Mathf.RoundToInt(responsibleness * missingQuota * 60F);

                            //            UCPlayer? assetWaster = UCPlayer.FromID(entry.Key);
                            //            if (assetWaster != null)
                            //                Points.AwardXP(assetWaster, penalty, Translation.Translate("xp_wasting_assets", assetWaster));
                            //        }
                            //    }
                            //}
                        
                        if (Data.Reporter is not null)
                        {
                            if (VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out Vehicles.VehicleSpawn spawn))
                                Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, spawn.SpawnPadInstanceID, vc.LastInstigator, vehicle.asset.GUID, vc.LastItem, vc.LastDamageOrigin, vehicleWasFriendly);
                            else
                                Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, uint.MaxValue, vc.LastInstigator, vehicle.asset.GUID, vc.LastItem, vc.LastDamageOrigin, vehicleWasFriendly);
                        }
                    }
                }
            }

            if (spotted != null)
                UnityEngine.Object.Destroy(spotted);
        }
        public static void OnRoundWin(ulong team)
        {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        /*
        float winMultiplier = 0.15f;

        List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team).ToList();

        for (int i = 0; i < players.Count; i++)
        {
            UCPlayer player = players[i];

            if (player.CSteamID.TryGetPlaytimeComponent(out PlaytimeComponent component) && component.stats is IExperienceStats exp)
            {
                //if (exp.XPGained > 0)
                //    Points.AwardXP(player.Player, Mathf.RoundToInt(exp.XPGained * winMultiplier), Translation.Translate("xp_victory", player.Steam64));

                if (exp.Credits > 0)
                    Points.AwardTW(player, Mathf.RoundToInt(exp.Credits * winMultiplier), Translation.Translate("xp_victory", player.Steam64));
            }
        }*/
    }
    public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
            UCPlayer? player = UCPlayer.FromPlayer(nelsonplayer);

            if (player == null) continue;
            int xp = Points.XPConfig.FlagCapturedXP;

            Points.AwardXP(player, player.NearbyMemberBonus(xp, 60), Translation.Translate("xp_flag_captured", player.Steam64));

            if (player.IsNearSquadLeader(50))
            {
                if (alreadyUpdated.TryGetValue(player.Squad!, out int amount))
                {
                    amount += Points.TWConfig.MemberFlagCapturePoints;
                }
                else
                {
                    alreadyUpdated.Add(player.Squad!, Points.TWConfig.MemberFlagCapturePoints);
                }
            }
        }
    }
    public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

        foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
        {
            UCPlayer? player = UCPlayer.FromPlayer(nelsonplayer);

            if (player == null) continue;
            int xp = Points.XPConfig.FlagNeutralizedXP;

            Points.AwardXP(player, xp, Translation.Translate("xp_flag_neutralized", player.Steam64));
            Points.AwardXP(player, player.NearbyMemberBonus(xp, 60) - xp, Translation.Translate("xp_squad_bonus", player.Steam64));
        }

        UpdateUITeam1(GetTeamBleed(1));
        UpdateUITeam2(GetTeamBleed(2));
    }
    public static void OnFlag20Seconds()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public static void OnCache20Seconds()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EffectManager.askEffectClearByID(config.Data.Team1TicketUIID, player.transportConnection);
        EffectManager.askEffectClearByID(config.Data.Team2TicketUIID, player.transportConnection);
        int bleed = GetTeamBleed(player.GetTeam());
        GetUIDisplayerInfo(player.GetTeam(), bleed, out ushort UIID, out string tickets, out string message);
        UpdateUI(player.transportConnection, UIID, tickets, message);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UIID = 0;
        tickets = "";
        message = "";

        if (TeamManager.IsTeam1(team))
        {
            tickets = Team1Tickets.ToString();
            UIID = config.Data.Team1TicketUIID;
        }

        else if (TeamManager.IsTeam2(team))
        {
            tickets = Team2Tickets.ToString();
            UIID = config.Data.Team2TicketUIID;
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
            tickets.ToString(Data.Locale),
            string.Empty,
            message
            );

    }
    public static void UpdateUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        GetUIDisplayerInfo(team, GetTeamBleed(team), out ushort id, out string tickets, out string message);
        UpdateUI(player.Connection, id, tickets, message);
    }
    public static void UpdateUITeam1(int bleed = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        GetUIDisplayerInfo(1, bleed, out ushort UIID, out string tickets, out string message);

        List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].HasUIHidden)
                UpdateUI(players[i].Connection, UIID, tickets, message);
        }
    }
    public static void UpdateUITeam2(int bleed = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        GetUIDisplayerInfo(2, bleed, out ushort UIID, out string tickets, out string message);

        List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].HasUIHidden)
                UpdateUI(players[i].Connection, UIID, tickets, message);
        }
    }
    public static int GetTeamBleed(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Is(out IFlagRotation fg))
        {
            if (Data.Is(out Invasion invasion))
            {
                if (team == invasion.AttackingTeam)
                {
                    int defenderFlags = fg.Rotation.Where(f => f.Owner == invasion.DefendingTeam).Count();

                    if (defenderFlags == fg.Rotation.Count)
                    {
                        return -1;
                    }
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
