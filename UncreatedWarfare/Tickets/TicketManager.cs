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
        internal static int _Team1previousTickets;
        internal static int _Team2previousTickets;
        public TicketManager()
        {
            TimeSinceMatchStart = DateTime.Now;

            Team1Tickets = config.data.StartingTickets;
            Team2Tickets = config.data.StartingTickets;
            _Team1previousTickets = config.data.StartingTickets;
            _Team2previousTickets = config.data.StartingTickets;

            

            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }
        public static void OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerDeathOffline(ulong deadteam)
        {
            if (deadteam == 1)
                AddTeam1Tickets(-1);
            else if (deadteam == 2)
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerSuicide(UCWarfare.SuicideEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);
        }
        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
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

            L.Log("attempting to award assist...");

            if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent component))
            {
                ulong killerID = parameters.killer.channel.owner.playerID.steamID.m_SteamID;
                ulong victimID = parameters.dead.channel.owner.playerID.steamID.m_SteamID;

                var assister = UCPlayer.FromID(component.secondLastAttacker.Key);
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
            Points.AwardXP(
                parameters.killer,
                Points.XPConfig.FriendlyKilledXP,
                Translation.Translate("xp_friendly_killed", parameters.killer));
        }
        private static void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ulong lteam = vehicle.lockedGroup.m_SteamID.GetTeam();

                if (lteam == 1)
                    AddTeam1Tickets(-1 * data.TicketCost);
                else if (lteam == 2)
                    AddTeam2Tickets(-1 * data.TicketCost);

                if (vehicle.transform.gameObject.TryGetComponent(out VehicleComponent vc))
                {
                    if (Points.XPConfig.VehicleDestroyedXP.ContainsKey(data.Type))
                    {
                        UCPlayer player = UCPlayer.FromID(vc.lastDamager);
                        bool wasCrashed = false;

                        if (player == null)
                            player = UCPlayer.FromSteamPlayer(vehicle.passengers[0].player);
                        if (player == null)
                            return;
                        else if (player.GetTeam() == vehicle.lockedGroup.m_SteamID)
                            wasCrashed = true;

                        ulong dteam = player.GetTeam();
                        bool vehicleWasEnemy = (dteam == 1 && lteam == 2) || (dteam == 2 && lteam == 1);
                        bool vehicleWasFriendly = dteam == lteam;
                        if (!vehicleWasFriendly)
                            Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);
                        if (!Points.XPConfig.VehicleDestroyedXP.TryGetValue(data.Type, out int amount))
                            amount = 0;
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

                        
                        if (vehicleWasEnemy)
                        {
                            Asset asset = Assets.find(vc.item);
                            string reason = "";
                            if (asset != null)
                            {
                                L.Log("     Asset was not null");

                                if (asset is ItemAsset item)
                                    reason = item.itemName;
                                else if (asset is VehicleAsset v)
                                    reason = "suicide " + v.vehicleName;
                            }

                            if (reason == "")
                                Chat.Broadcast("VEHICLE_DESTROYED_UNKNOWN", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName);
                            else
                                Chat.Broadcast("VEHICLE_DESTROYED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), vehicle.asset.vehicleName, reason);

                            Points.AwardXP(player, amount, Translation.Translate("xp_" + message, player));
                            Points.TryAwardDriverAssist(player.Player, amount, data.TicketCost);
                            Stats.StatsManager.ModifyStats(player.Steam64, s => s.VehiclesDestroyed++, false);
                            Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesDestroyed++);
                        }
                        else if (vehicleWasFriendly)
                        {
                            Chat.Broadcast("VEHICLE_TEAMKILLED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), "", vehicle.asset.vehicleName);

                            if (message != string.Empty) message = "xp_friendly_" + message;
                            Points.AwardXP(player.Player, -amount, Translation.Translate(message, player.Steam64));
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
                                    var passenger = vehicle.passengers[i];

                                    if (passenger.player is not null)
                                    {
                                        vc.EvaluateUsage(passenger.player);
                                    }
                                }

                                double totalTime = 0;
                                foreach (var entry in vc.UsageTable)
                                    totalTime += entry.Value;

                                foreach (var entry in vc.UsageTable)
                                {
                                    float responsibleness = (float)(entry.Value / totalTime);
                                    int penalty = Mathf.RoundToInt(responsibleness * missingQuota * 60F);

                                    L.Log($"    {entry.Key} was responsible for {responsibleness * 100}% of the damage. Their penalty: {penalty} XP");

                                    var assetWaster = UCPlayer.FromID(entry.Key);
                                    if (assetWaster != null)
                                        Points.AwardXP(assetWaster, penalty, "xp_wasting_assets");
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
            float winMultiplier = 0.15f;

            List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UCPlayer player = players[i];

                if (player.CSteamID.TryGetPlaytimeComponent(out PlaytimeComponent component) && component.stats is IExperienceStats exp)
                {
                    if (exp.XPGained > 0)
                        Points.AwardXP(player.Player, Mathf.RoundToInt(exp.XPGained * winMultiplier), Translation.Translate("xp_victory", player.Steam64));

                    if (player.IsSquadLeader())
                    {
                        if (exp.OFPGained > 0)
                            Points.AwardTW(player.Squad.Leader.Player, Mathf.RoundToInt(exp.OFPGained * winMultiplier), "");
                    }
                }
            }
        }
        public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
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
            else if (Data.Is<TeamCTF>(out _))
            {
                if (capturedTeam == 1 && !flag.HasBeenCapturedT1)
                {
                    Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2 && !flag.HasBeenCapturedT2)
                {
                    Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }

                if (lostTeam == 1)
                    Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
                if (lostTeam == 2)
                    Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
            }
            else
            {
                if (capturedTeam == 1 && !flag.HasBeenCapturedT1)
                {
                    Team1Tickets += config.data.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2 && !flag.HasBeenCapturedT2)
                {
                    Team2Tickets += config.data.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }

                if (lostTeam == 1)
                    Team1Tickets += config.data.TicketsFlagLost;
                if (lostTeam == 2)
                    Team2Tickets += config.data.TicketsFlagLost;
            }
            

            UpdateUITeam1();
            UpdateUITeam2();

            Dictionary<Squad, int> alreadyUpdated = new Dictionary<Squad, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = Points.XPConfig.FlagCapturedXP;

                Points.AwardXP(player, player.NearbyMemberBonus(xp, 50), Translation.Translate("xp_flag_captured", player.Steam64));

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
            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = Points.XPConfig.FlagNeutralizedXP;

                Points.AwardXP(player, xp, Translation.Translate("xp_flag_neutralized", player.Steam64));
                Points.AwardXP(player, player.NearbyMemberBonus(xp, 150) - xp, Translation.Translate("xp_squad_bonus", player.Steam64));
            }
        }
        public static void OnFlagTick()
        {
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
        public static void OnPlayerJoined(UCPlayer player)
        {
            ulong team = player.GetTeam();
            GetTeamBleed(team, out int bleed, out string message);
            UpdateUI(player.Player.channel.owner.transportConnection, team, bleed, Translation.Translate(message, player));
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            EffectManager.askEffectClearByID(config.data.Team1TicketUIID, player.transportConnection);
            EffectManager.askEffectClearByID(config.data.Team2TicketUIID, player.transportConnection);
            GetTeamBleed(newGroup, out int bleed, out string message);
            UpdateUI(player.transportConnection, newGroup, bleed, Translation.Translate(message, player));
        }
        public static void OnStagingPhaseEnded()
        {
            TimeSinceMatchStart = DateTime.Now;
        }
        public static void OnNewGameStarting()
        {
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
                Team1Tickets = config.data.StartingTickets;
                Team2Tickets = config.data.StartingTickets;
            }

            UpdateUITeam1();
            UpdateUITeam2();
        }

        public static void AddTeam1Tickets(int number)
        {
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 1)
                return;

            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                Data.Gamemode.DeclareWin(2);
            }
            UpdateUITeam1();
        }
        public static void AddTeam2Tickets(int number)
        {
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 2)
                return;

            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                Data.Gamemode.DeclareWin(1);
            }
            UpdateUITeam2();
        }
        public static void UpdateUI(ITransportConnection connection, ulong team, int bleed, string message)
        {
            ushort UIID = 0;
            int tickets = 0;
            if (TeamManager.IsTeam1(team))
            {
                tickets = Team1Tickets;
                UIID = config.data.Team1TicketUIID;
            }

            else if (TeamManager.IsTeam2(team))
            {
                tickets = Team2Tickets;
                UIID = config.data.Team2TicketUIID;
            }

            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == team)
            {
                EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                insurgency.CachesLeft.ToString(Data.Locale) + " Caches", "", "");
            }
            else
            {
                EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                tickets.ToString(Data.Locale),
                bleed < 0 ? bleed.ToString(Data.Locale) : string.Empty,
                message
                );
            }
                
        }
        public static void UpdateUITeam1()
        {
            GetTeamBleed(TeamManager.Team1ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team1ID, bleed, Translation.Translate(message, players[i]));
            }
        }
        public static void UpdateUITeam2()
        {
            GetTeamBleed(TeamManager.Team2ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team2ID, bleed, Translation.Translate(message, players[i]));
            }
        }
        public static void GetTeamBleed(ulong team, out int bleed, out string message)
        {
            if (Data.Is(out IFlagRotation fg))
            {
                if (Data.Is(out Invasion invasion) && team == invasion.AttackingTeam)
                {
                    int defenderFlags = fg.Rotation.Where(f => f.Owner == invasion.DefendingTeam).Count();

                    if (defenderFlags == fg.Rotation.Count)
                    {
                        bleed = -1;
                        message = bleed.ToString() + "BLEEDING TICKETS";
                    }
                }
            }

            bleed = 0;
            message = "";
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
        public int StartingTickets;
        public int TicketHandicapDifference;
        public int FOBCost;
        public int TicketsFlagCaptured;
        public int TicketsFlagLost;
        public ushort Team1TicketUIID;
        public ushort Team2TicketUIID;

        public override void SetDefaults()
        {
            StartingTickets = 200;
            TicketHandicapDifference = 40;
            FOBCost = 15;
            TicketsFlagCaptured = 20;
            TicketsFlagLost = -20;
            Team1TicketUIID = 36035;
            Team2TicketUIID = 36034;
        }
        public TicketData() { }
    }
}
