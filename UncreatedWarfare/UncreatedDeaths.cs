using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using UnityEngine;

namespace Uncreated.Warfare
{
    partial class UCWarfare
    {
        public delegate void PlayerDeathHandler(DeathEventArgs death);
        public static event PlayerDeathHandler OnPlayerDeathGlobal;
        public event Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerDeath OnPlayerDeathPostMessages;
        private void Teamkill(KillEventArgs parameters)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            L.Log(Translation.Translate("teamkilled_console_log", 0,
                F.GetPlayerOriginalNames(parameters.killer).PlayerName,
                parameters.killer.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale),
                F.GetPlayerOriginalNames(parameters.dead).PlayerName,
                parameters.dead.channel.owner.playerID.steamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
            byte team = parameters.killer.GetTeamByte();
            if (team == 1 || team == 2)
            {
                TicketManager.OnFriendlyKilled(parameters);
                Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team);
                StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, s => s.Teamkills++, false);
                StatsManager.ModifyTeam(team, t => t.Teamkills++, false);
                if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is ITeamPVPModeStats tpvp)
                    tpvp.AddTeamkill();
                if (Configuration.Instance.AdminLoggerSettings.LogTKs)
                {
                    Asset a = Assets.find(parameters.item);
                    Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                        parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                        parameters.key, parameters.itemName, a == null ? (ushort)0 : a.id, parameters.distance);
                }
                Invocations.Shared.LogTeamkilled.NetInvoke(parameters.killer.channel.owner.playerID.steamID.m_SteamID, parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                    parameters.key, parameters.itemName, DateTime.Now);
                StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, x => x.Teamkills++, false);
                Data.Reporter.OnTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, parameters.item, parameters.dead.channel.owner.playerID.steamID.m_SteamID, parameters.cause);
                if (Data.Gamemode is TeamCTF ctf)
                {
                    if (team == 1)
                        ctf.GameStats.teamkillsT1++;
                    else
                        ctf.GameStats.teamkillsT2++;
                }
                else if (Data.Gamemode is Invasion inv)
                {
                    if (team == 1)
                        inv.GameStats.teamkillsT1++;
                    else
                        inv.GameStats.teamkillsT2++;
                }
                else if (Data.Gamemode is Insurgency ins)
                {
                    if (team == 1)
                        ins.GameStats.teamkillsT1++;
                    else
                        ins.GameStats.teamkillsT2++;
                }
            }
        }
        public class KillEventArgs
        {
            public Player killer;
            public Player dead;
            public Player? LandmineLinkedAssistant;
            public EDeathCause cause;
            public Guid item;
            public string itemName;
            public string key;
            public ELimb limb;
            public float distance;
            public bool teamkill;
            public string kitname;
            public ushort turretOwner;
            public override string ToString()
            {
                string msg;
                if (cause == EDeathCause.LANDMINE)
                {
                    if (LandmineLinkedAssistant == null)
                    {
                        Chat.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            new FPlayerName() { CharacterName = "Unknown", NickName = "Unknown", PlayerName = "Unknown", Steam64 = 0 }, 0, limb, itemName, out msg, false);
                    }
                    else
                    {
                        Chat.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            F.GetPlayerOriginalNames(LandmineLinkedAssistant), LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                }
                else
                {
                    Chat.BroadcastDeath(key, cause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), false, killer.GetTeam(), limb, itemName, distance, out msg, false);
                }
                return msg;
            }
        }
        private void Kill(KillEventArgs parameters)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            //L.Log("[KILL] " + parameters.ToString(), ConsoleColor.Blue);
            byte team = parameters.killer.GetTeamByte();
            if (team == 1 || team == 2)
            {
                TicketManager.OnEnemyKilled(parameters);
                Data.DatabaseManager.AddKill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team);
                if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent c))
                {
                    if (c.stats is IPVPModeStats kd)
                        kd.AddKill();
                    if (c.stats is BaseCTFStats st && parameters.killer.IsOnFlag())
                        st.AddKillOnPoint();
                }

                QuestManager.OnKill(parameters);
                bool atk = false;
                bool def = false;
                if (Data.Is(out IGameStats ws) && ws.GameStats is ILongestShotTracker ls)
                {
                    if (ws.GameStats != null && parameters.cause == EDeathCause.GUN && (ls.LongestShot.Player == 0 || ls.LongestShot.Distance < parameters.distance))
                    {
                        ls.LongestShot = new LongestShot()
                        {
                            Player = parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                            Distance = parameters.distance,
                            Gun = parameters.item,
                            Team = team
                        };
                    }
                }
                if (Data.Is(out Insurgency ins))
                {
                    if (team == ins.DefendingTeam)
                    {
                        for (int i = 0; i < ins.Caches.Count; i++)
                        {
                            Insurgency.CacheData d = ins.Caches[i];
                            if (d.IsActive && !d.IsDestroyed && d.Cache != null && d.Cache.Structure != null &&
                                (d.Cache.Structure.model.transform.position - parameters.killer.transform.position).sqrMagnitude <=
                                Gamemode.ConfigObj.data.Insurgency.CacheDiscoverRange * Gamemode.ConfigObj.data.Insurgency.CacheDiscoverRange)
                            {
                                if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent comp) && comp.stats is InsurgencyPlayerStats ps) ps._killsDefense++;
                            }
                        }
                    }
                    else if (team == ins.AttackingTeam && parameters.dead != null)
                    {
                        for (int i = 0; i < ins.Caches.Count; i++)
                        {
                            Insurgency.CacheData d = ins.Caches[i];
                            if (d.IsActive && !d.IsDestroyed && d.Cache != null && d.Cache.Structure != null &&
                                (d.Cache.Structure.model.transform.position - parameters.dead.transform.position).sqrMagnitude <=
                                Gamemode.ConfigObj.data.Insurgency.CacheDiscoverRange * Gamemode.ConfigObj.data.Insurgency.CacheDiscoverRange)
                            {
                                if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent comp) && comp.stats is InsurgencyPlayerStats ps) ps._killsAttack++;
                            }
                        }
                    }
                }
                if (Data.Is(out IFlagRotation fg))
                {
                    try
                    {
                        for (int f = 0; f < fg.Rotation.Count; f++)
                        {
                            Gamemodes.Flags.Flag flag = fg.Rotation[f];
                            if (flag.ZoneData.IsInside(parameters.killer.transform.position))
                            {
                                def = flag.IsContested(out ulong winner) || winner != team;
                                atk = !def;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error checking defending/attacking status on kill.");
                        L.LogError(ex);
                    }
                }
                StatsManager.ModifyTeam(team, t => t.Kills++, false);
                if (parameters.turretOwner != 0 && Assets.find(EAssetType.VEHICLE, parameters.turretOwner) is VehicleAsset vasset && vasset != null)
                    StatsManager.ModifyVehicle(parameters.turretOwner, v => v.KillsWithGunner++);
                if (KitManager.HasKit(parameters.killer, out Kit kit))
                {
                    StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, s =>
                    {
                        s.Kills++;
                        WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                        if (kitData == default)
                        {
                            kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = team, Kills = 1 };
                            if (parameters.cause == EDeathCause.GUN)
                                kitData.AverageGunKillDistance = (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter + parameters.distance) / ++kitData.AverageGunKillDistanceCounter;
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Kills++;
                            if (parameters.cause == EDeathCause.GUN)
                                kitData.AverageGunKillDistance = (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter + parameters.distance) / ++kitData.AverageGunKillDistanceCounter;
                        }
                        if (atk)
                        {
                            s.KillsWhileAttackingFlags++;
                        }
                        else if (def)
                        {
                            s.KillsWhileDefendingFlags++;
                        }
                    }, false);
                }
                else
                    StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, s =>
                    { s.Kills++; if (atk) s.KillsWhileAttackingFlags++; else if (def) s.KillsWhileDefendingFlags++; }, false);
                if (KitManager.KitExists(parameters.kitname, out kit) && parameters.cause != EDeathCause.VEHICLE && parameters.cause != EDeathCause.ROADKILL && Assets.find(parameters.item) is ItemAsset asset)
                {
                    StatsManager.ModifyWeapon(asset.id, kit.Name, x =>
                    {
                        x.Kills++;
                        if (parameters.limb == ELimb.SKULL)
                            x.SkullKills++;
                        else if (parameters.limb == ELimb.SPINE || parameters.limb == ELimb.LEFT_FRONT || parameters.limb == ELimb.RIGHT_FRONT ||
                                 parameters.limb == ELimb.LEFT_BACK || parameters.limb == ELimb.RIGHT_BACK)
                            x.BodyKills++;
                        else if (parameters.limb == ELimb.LEFT_HAND || parameters.limb == ELimb.RIGHT_HAND || parameters.limb == ELimb.LEFT_ARM || parameters.limb == ELimb.RIGHT_ARM)
                            x.ArmKills++;
                        else if (parameters.limb == ELimb.LEFT_FOOT || parameters.limb == ELimb.RIGHT_FOOT || parameters.limb == ELimb.LEFT_LEG || parameters.limb == ELimb.RIGHT_LEG)
                            x.LegKills++;
                        x.AverageKillDistance = (x.AverageKillDistance * x.AverageKillDistanceCounter + parameters.distance) / ++x.AverageKillDistanceCounter;
                    }, true);
                    StatsManager.ModifyKit(kit.Name, k =>
                    {
                        k.Kills++;
                        if (parameters.cause == EDeathCause.GUN)
                            k.AverageGunKillDistance = (k.AverageGunKillDistance * k.AverageGunKillDistanceCounter + parameters.distance) / ++k.AverageGunKillDistanceCounter;
                    }, true);
                }
            }
        }
        public class SuicideEventArgs
        {
            public Player dead;
            public EDeathCause cause;
            public Guid item;
            public string itemName;
            public string key;
            public ELimb limb;
            public float distance;
            public override string ToString()
            {
                string msg;
                FPlayerName name = F.GetPlayerOriginalNames(dead);
                ulong team = dead.GetTeam();
                if (cause == EDeathCause.LANDMINE)
                    Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                else
                    Chat.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg, false);
                return msg;
            }
            public DeathEventArgs DeadArgs
            {
                get => new DeathEventArgs()
                {
                    cause = cause,
                    dead = dead,
                    distance = distance,
                    item = item,
                    itemName = itemName,
                    key = key,
                    killerargs = null,
                    limb = limb
                };
            }
        }
        public void Suicide(SuicideEventArgs parameters)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            //L.Log("[SUICIDE] " + parameters.ToString(), ConsoleColor.Blue);
            DeathEventArgs args = new DeathEventArgs
            {
                cause = parameters.cause,
                killerargs = null,
                dead = parameters.dead,
                distance = parameters.distance,
                item = parameters.item,
                itemName = parameters.itemName,
                key = parameters.key,
                limb = parameters.limb
            };
            OnPlayerDeathGlobal?.Invoke(args);
            Data.Gamemode?.OnPlayerDeath(args);

            byte team = parameters.dead.GetTeamByte();
            if (team == 1 || team == 2)
            {
                TicketManager.OnPlayerSuicide(parameters);
                Data.DatabaseManager.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team);
                StatsManager.ModifyTeam(team, t => t.Deaths++, false);
                QuestManager.OnDeath(parameters);
                if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IPVPModeStats kd)
                    kd.AddDeath();
                if (KitManager.HasKit(parameters.dead, out Kit kit))
                {
                    StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s =>
                    {
                        s.Deaths++;
                        WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                        if (kitData == default)
                        {
                            kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = team, Deaths = 1 };
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Deaths++;
                        }
                    }, false);
                    StatsManager.ModifyKit(kit.Name, k => k.Deaths++, true);
                    if (Assets.find(parameters.item) is ItemAsset asset)
                        StatsManager.ModifyWeapon(asset.id, kit.Name, w => w.Deaths++, true);
                }
                else
                    StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s => s.Deaths++, false);
                if (Data.Gamemode is TeamCTF ctf)
                {
                    if (team == 1)
                    {
                        ctf.GameStats.casualtiesT1++;
                    }
                    else
                    {
                        ctf.GameStats.casualtiesT2++;
                    }
                }
            }
        }
        public class DeathEventArgs
        {
            public Player dead;
            public KillEventArgs? killerargs;
            public EDeathCause cause;
            public Guid item;
            public string itemName;
            public string key;
            public ELimb limb;
            public float distance;
            public override string ToString()
            {
                string msg;
                FPlayerName name = F.GetPlayerOriginalNames(dead);
                ulong team = dead.GetTeam();
                if (cause == EDeathCause.LANDMINE)
                {
                    if (killerargs == default)
                    {
                        Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                    }
                    else
                    {
                        if (killerargs.LandmineLinkedAssistant == default)
                        {
                            if (killerargs.killer != default)
                            {
                                FPlayerName name2 = F.GetPlayerOriginalNames(killerargs.killer);
                                ulong team2 = killerargs.killer.GetTeam();
                                Chat.BroadcastLandmineDeath(key, name, team, name2, team2, name2, team2, limb, itemName, out msg, false);
                            }
                            else
                            {
                                Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                            }
                        }
                        else
                        {
                            Chat.BroadcastLandmineDeath(key, name, team, F.GetPlayerOriginalNames(killerargs.killer ?? dead), (killerargs.killer ?? dead).GetTeam(),
                            F.GetPlayerOriginalNames(killerargs.LandmineLinkedAssistant), killerargs.LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                        }
                    }
                }
                else
                {
                    if (killerargs == default || killerargs.killer == default)
                    {
                        Chat.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg, false);
                    }
                    else
                    {
                        Chat.BroadcastDeath(key, cause, name, team, F.GetPlayerOriginalNames(killerargs.killer), false, killerargs.killer.GetTeam(), limb, itemName, distance, out msg, false);
                    }
                }
                return msg;
            }
        }
        public void DeathNotSuicide(DeathEventArgs parameters)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            //L.Log("[DEATH] " + parameters.ToString(), ConsoleColor.Blue);

            byte team = parameters.dead.GetTeamByte();
            Data.Gamemode?.OnPlayerDeath(parameters);
            if (team == 1 || team == 2)
            {
                if (Data.Gamemode is TeamCTF ctf)
                {
                    if (team == 1)
                    {
                        ctf.GameStats.casualtiesT1++;
                    }
                    else
                    {
                        ctf.GameStats.casualtiesT2++;
                    }
                }
                TicketManager.OnPlayerDeath(parameters);
                if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is IPVPModeStats kd)
                    kd.AddDeath();
                Data.DatabaseManager?.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team);
                QuestManager.OnDeath(parameters);
                StatsManager.ModifyTeam(team, t => t.Deaths++, false);
                if (KitManager.HasKit(parameters.dead, out Kit kit))
                {
                    StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s =>
                    {
                        s.Deaths++;
                        WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                        if (kitData == default)
                        {
                            kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = team, Deaths = 1 };
                            s.Kits.Add(kitData);
                        }
                        else
                        {
                            kitData.Deaths++;
                        }
                    }, false);
                    ItemJar primary = parameters.dead.inventory.items[(int)InventoryGroup.Primary].items.FirstOrDefault();
                    ItemJar secondary = parameters.dead.inventory.items[(int)InventoryGroup.Secondary].items.FirstOrDefault();
                    if (primary != null)
                        StatsManager.ModifyWeapon(primary.item.id, kit.Name, x => x.Deaths++, true);
                    if (secondary != null && (primary == null || primary.item.id != secondary.item.id)) // prevents 2 of the same gun from counting twice
                        StatsManager.ModifyWeapon(secondary.item.id, kit.Name, x => x.Deaths++, true);
                    StatsManager.ModifyKit(kit.Name, k => k.Deaths++, true);
                }
                else
                    StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s => s.Deaths++, false);
            }
            OnPlayerDeathGlobal?.Invoke(parameters);
        }
        private void OnPlayerDeath(UnturnedPlayer dead, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? ucplayer = UCPlayer.FromUnturnedPlayer(dead);
            if (ucplayer != null)
                ucplayer.LifeCounter++;

            if (cause == EDeathCause.LANDMINE)
            {
                SteamPlayer placer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                Player? triggerer;
                FPlayerName placerName;
                FPlayerName triggererName;
                bool foundPlacer;
                bool foundTriggerer;
                ulong deadTeam = dead.GetTeam();
                ulong placerTeam;
                ulong triggererTeam;
                Guid landmineID;
                LandmineData landmine;
                string landmineName;
                if (placer == null)
                {
                    L.Log("placer is null");
                    placer = dead.Player.channel.owner;
                    placerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    foundPlacer = false;
                    landmineID = Guid.Empty;
                    landmineName = "Unknown";
                    landmine = LandmineData.Nil;
                    placerTeam = 0;
                    triggererTeam = 0;
                    triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    triggerer = null;
                    foundTriggerer = false;
                }
                else
                {
                    placerName = F.GetPlayerOriginalNames(placer);
                    placerTeam = placer.GetTeam();
                    foundPlacer = true;
                    if (placer.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                    {
                        if (c.LastLandmineExploded.Equals(default(LandmineData)) || c.LastLandmineExploded.owner == null)
                        {
                            landmine = LandmineData.Nil;
                            landmineID = Guid.Empty;
                        }
                        else
                        {
                            landmine = c.LastLandmineExploded;
                            landmineID = c.LastLandmineExploded.barricadeGUID;
                        }
                    }
                    else
                    {
                        landmineID = Guid.Empty;
                        landmine = LandmineData.Nil;
                    }
                    if (landmineID != Guid.Empty)
                    {
                        if (Assets.find(landmineID) is ItemAsset asset) landmineName = asset.itemName;
                        else landmineName = landmineID.ToString("N");
                    }
                    else landmineName = "Unknown";
                    if (landmine.instanceID != 0)
                    {
                        PlaytimeComponent pt = Data.PlaytimeComponents.Values.FirstOrDefault(
                            x =>
                            x != null &&
                            !x.LastLandmineTriggered.Equals(default(LandmineData)) &&
                            landmine.instanceID == x.LastLandmineTriggered.instanceID);
                        if (pt != null)
                        {
                            triggerer = pt.player;
                            triggererTeam = triggerer.GetTeam();
                            triggererName = F.GetPlayerOriginalNames(triggerer);
                            foundTriggerer = true;
                        }
                        else
                        {
                            triggerer = null;
                            triggererTeam = 0;
                            triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                            foundTriggerer = false;
                        }
                    }
                    else
                    {
                        triggerer = null;
                        triggererTeam = 0;
                        triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                        foundTriggerer = false;
                    }
                    L.Log($"Triggerer: {(foundTriggerer ? "not found" : triggererName.PlayerName)}");
                }
                string key = "LANDMINE";
                string itemkey = landmineID.ToString("N");
                if (foundPlacer && placer.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                {
                    key += "_SUICIDE";
                }
                if (landmineID == Guid.Empty)
                {
                    key += "_UNKNOWN";
                }
                if (foundTriggerer && triggerer!.channel.owner.playerID.steamID.m_SteamID != dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID != placer.playerID.steamID.m_SteamID)
                {
                    key += "_TRIGGERED";
                }
                if (!foundPlacer)
                {
                    key += "_UNKNOWNKILLER";
                }
                L.Log(key);
                if (foundPlacer && foundTriggerer)
                {
                    if (triggerer!.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID == placer.playerID.steamID.m_SteamID)
                    {
                        if (Config.DeathMessages.PenalizeSuicides)
                            Suicide(new SuicideEventArgs()
                            {
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb
                            });
                    }
                    else if (placerTeam == triggererTeam)
                    {
                        if (deadTeam == placerTeam)
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = triggerer,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = placer.player,
                                distance = 0,
                                teamkill = true,
                                kitname = string.Empty
                            };
                            Teamkill(a);
                            if (Config.DeathMessages.PenalizeTeamkilledPlayers)
                                DeathNotSuicide(new DeathEventArgs()
                                {
                                    killerargs = a,
                                    cause = cause,
                                    dead = dead.Player,
                                    distance = 0,
                                    limb = limb,
                                    item = landmineID,
                                    itemName = landmineName,
                                    key = key
                                });
                        }
                        else
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = placer.player,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = triggerer,
                                distance = 0,
                                teamkill = false,
                                kitname = string.Empty
                            };
                            Kill(a);
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                killerargs = a,
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                limb = limb,
                                item = landmineID,
                                itemName = landmineName,
                                key = key
                            });
                        }
                    }
                    else
                    {
                        if (deadTeam == placerTeam) // and placer team != triggerer team
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = triggerer,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = placer.player,
                                distance = 0,
                                teamkill = false,
                                kitname = string.Empty
                            };
                            Kill(a);
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                killerargs = a,
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                limb = limb,
                                item = landmineID,
                                itemName = landmineName,
                                key = key
                            });
                        }
                        else // dead team == triggerer team
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = placer.player,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = triggerer,
                                distance = 0,
                                teamkill = false,
                                kitname = string.Empty
                            };
                            Kill(a);
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                killerargs = a,
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                limb = limb,
                                item = landmineID,
                                itemName = landmineName,
                                key = key
                            });
                        }
                    }
                }
                else if (foundPlacer)
                {
                    if (dead.Player.channel.owner.playerID.steamID.m_SteamID == placer.playerID.steamID.m_SteamID)
                    {
                        if (Config.DeathMessages.PenalizeSuicides)
                            Suicide(new SuicideEventArgs()
                            {
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb
                            });
                    }
                    else if (deadTeam == placerTeam)
                    {
                        KillEventArgs a = new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = placer.player,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0,
                            teamkill = true,
                            kitname = string.Empty
                        };
                        Teamkill(a);
                        if (Config.DeathMessages.PenalizeTeamkilledPlayers)
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                killerargs = a,
                                cause = cause,
                                limb = limb,
                                dead = dead.Player,
                                distance = 0,
                                item = landmineID,
                                itemName = landmineName,
                                key = key
                            });
                    }
                    else
                    {
                        KillEventArgs a = new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = placer.player,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0,
                            teamkill = false,
                            kitname = string.Empty
                        };
                        Kill(a);
                        DeathNotSuicide(new DeathEventArgs()
                        {
                            killerargs = a,
                            cause = cause,
                            limb = limb,
                            dead = dead.Player,
                            distance = 0,
                            item = landmineID,
                            itemName = landmineName,
                            key = key
                        });
                    }
                }
                else if (foundTriggerer)
                {
                    if (triggerer!.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                    {
                        if (Config.DeathMessages.PenalizeSuicides)
                            Suicide(new SuicideEventArgs()
                            {
                                cause = cause,
                                dead = dead.Player,
                                distance = 0,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb
                            });
                    }
                    else if (deadTeam == triggererTeam)
                    {
                        KillEventArgs a = new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = triggerer,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0,
                            teamkill = true,
                            kitname = string.Empty
                        };
                        Teamkill(a);
                        if (Config.DeathMessages.PenalizeTeamkilledPlayers)
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                killerargs = a,
                                cause = cause,
                                limb = limb,
                                dead = dead.Player,
                                distance = 0,
                                item = landmineID,
                                itemName = landmineName,
                                key = key
                            });
                    }
                    else
                    {
                        KillEventArgs a = new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = triggerer,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0,
                            teamkill = false,
                            kitname = string.Empty
                        };
                        Kill(a);
                        DeathNotSuicide(new DeathEventArgs()
                        {
                            killerargs = a,
                            cause = cause,
                            limb = limb,
                            dead = dead.Player,
                            distance = 0,
                            item = landmineID,
                            itemName = landmineName,
                            key = key
                        });
                    }
                }
                LogLandmineMessage(key, dead.Player, placerName, placerTeam, limb, landmineName, triggererName, triggererTeam);
            }
            else
            {
                SteamPlayer? killer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                FPlayerName killerName;
                bool foundKiller;
                Guid item;
                string? itemName = null;
                float distance = 0f;
                bool translateName = false;
                ulong killerTeam;
                ushort turretOwner = 0;
                string kitname;
                bool itemIsVehicle = cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL;
                if (killer == null)
                {
                    if (cause != EDeathCause.ZOMBIE && Data.Is(out IRevives r) && r.ReviveManager.DeathInfo.TryGetValue(dead.CSteamID.m_SteamID, out DeathInfo info))
                    {
                        item = info.item;
                        distance = info.distance;
                        killerName = info.killerName;
                        killerTeam = info.killerTeam;
                        kitname = info.kitName;
                        foundKiller = false;
                        bool foundvehasset = true;
                        if (itemIsVehicle)
                        {
                            if (Assets.find(item) is VehicleAsset asset) itemName = asset.vehicleName;
                            else
                            {
                                itemName = item.ToString("N");
                                foundvehasset = false;
                            }
                        }
                        if (!itemIsVehicle || !foundvehasset)
                        {
                            if (Assets.find(item) is ItemAsset asset) itemName = asset.itemName;
                            else itemName = item.ToString("N");
                        }

                    }
                    else
                    {
                        killer = dead.Player.channel.owner;
                        if (cause == EDeathCause.ZOMBIE)
                        {
                            killerName = new FPlayerName() { CharacterName = "zombie", PlayerName = "zombie", NickName = "zombie", Steam64 = 0 };
                            killerTeam = TeamManager.ZOMBIE_TEAM_ID;
                            translateName = true;
                        }
                        else
                        {
                            killerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                            killerTeam = 0;
                        }
                        foundKiller = false;
                        kitname = string.Empty;
                        item = Guid.Empty;
                        itemName = "Unknown";
                    }
                }
                else
                {
                    killerName = F.GetPlayerOriginalNames(killer);
                    killerTeam = killer.GetTeam();
                    foundKiller = true;
                    try
                    {
                        if (!Data.Is(out IRevives r) || !r.ReviveManager.DeathInfo.TryGetValue(dead.CSteamID.m_SteamID, out DeathInfo info))
                            GetKillerInfo(out item, out distance, out _, out _, out kitname, out turretOwner, cause, killer, dead.Player);
                        else
                        {
                            item = info.item;
                            distance = info.distance;
                            if (KitManager.HasKit(killer, out Kit kit))
                                kitname = kit.Name;
                            else kitname = killerTeam == 0 ? string.Empty : (killerTeam == 1 ? TeamManager.Team1UnarmedKit : (killerTeam == 2 ? TeamManager.Team2UnarmedKit : string.Empty));
                            turretOwner = info.vehicle;
                        }
                    }
                    catch { item = Guid.Empty; kitname = string.Empty; }
                    if (item != Guid.Empty)
                    {
                        if (itemIsVehicle)
                        {
                            if (Assets.find(item) is VehicleAsset asset) itemName = asset.vehicleName;
                            else itemName = item.ToString("N");
                        }
                        else
                        {
                            if (Assets.find(item) is ItemAsset asset) itemName = asset.itemName;
                            else itemName = item.ToString("N");
                        }
                    }
                    else itemName = "Unknown";
                }
                string key = cause.ToString();
                if (dead.CSteamID.m_SteamID == murderer.m_SteamID && cause != EDeathCause.SUICIDE) key += "_SUICIDE";
                if (cause == EDeathCause.ARENA && Data.DeathLocalization[JSONMethods.DEFAULT_LANGUAGE].ContainsKey("MAINCAMP")) key = "MAINCAMP";
                else if (cause == EDeathCause.ACID && Data.DeathLocalization[JSONMethods.DEFAULT_LANGUAGE].ContainsKey("MAINDEATH")) key = "MAINDEATH";
                if ((cause == EDeathCause.GUN || cause == EDeathCause.MELEE || cause == EDeathCause.MISSILE || cause == EDeathCause.SPLASH
                    || cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL || cause == EDeathCause.BLEEDING) && foundKiller)
                {
                    if (item != Guid.Empty)
                    {
                        Asset a = Assets.find(item);
                        string k1 = (itemIsVehicle ? "v" : "") + a == null ? "0" : a.id.ToString(Data.Locale);
                        string k2 = k1 + "_SUICIDE";
                        if (Data.DeathLocalization[JSONMethods.DEFAULT_LANGUAGE].ContainsKey(k1))
                        {
                            key = k1;
                        }
                        if (dead.CSteamID.m_SteamID == killer!.playerID.steamID.m_SteamID && cause != EDeathCause.SUICIDE && Data.DeathLocalization[JSONMethods.DEFAULT_LANGUAGE].ContainsKey(k2))
                        {
                            key = k2;
                        }
                    }
                    else
                    {
                        key += "_UNKNOWN";
                    }
                    if (cause == EDeathCause.BLEEDING)
                    {
                        if (murderer == Provider.server)
                            key += "_SUICIDE";
                        else if (!murderer.m_SteamID.ToString(Data.Locale).StartsWith("765"))
                            killerName = new FPlayerName() { CharacterName = "zombie", NickName = "zombie", PlayerName = "zombie", Steam64 = murderer == default || murderer == CSteamID.Nil ? 0 : murderer.m_SteamID };
                    }
                }
                if (itemName == null) itemName = item.ToString();
                if (foundKiller)
                {
                    if (killer!.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                    {
                        if (Config.DeathMessages.PenalizeSuicides)
                            Suicide(new SuicideEventArgs()
                            {
                                cause = cause,
                                dead = dead.Player,
                                distance = distance,
                                item = item,
                                itemName = itemName,
                                key = key,
                                limb = limb
                            });
                    }
                    else
                    {
                        if (killerTeam != dead.GetTeam())
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                LandmineLinkedAssistant = default,
                                cause = cause,
                                dead = dead.Player,
                                distance = distance,
                                item = item,
                                itemName = itemName,
                                key = key,
                                killer = killer.player,
                                limb = limb,
                                teamkill = false,
                                kitname = kitname,
                                turretOwner = turretOwner
                            };
                            Kill(a);
                            DeathNotSuicide(new DeathEventArgs()
                            {
                                limb = limb,
                                cause = cause,
                                dead = dead.Player,
                                distance = distance,
                                item = item,
                                itemName = itemName,
                                key = key,
                                killerargs = a
                            });
                        }
                        else
                        {
                            KillEventArgs a = new KillEventArgs()
                            {
                                LandmineLinkedAssistant = default,
                                cause = cause,
                                dead = dead.Player,
                                distance = distance,
                                item = item,
                                itemName = itemName,
                                key = key,
                                killer = killer.player,
                                limb = limb,
                                teamkill = true,
                                kitname = kitname,
                                turretOwner = turretOwner
                            };
                            Teamkill(a);
                            if (Config.DeathMessages.PenalizeTeamkilledPlayers)
                                DeathNotSuicide(new DeathEventArgs()
                                {
                                    limb = limb,
                                    cause = cause,
                                    dead = dead.Player,
                                    distance = distance,
                                    item = item,
                                    itemName = itemName,
                                    key = key,
                                    killerargs = a
                                });
                        }
                    }
                }
                else
                {
                    DeathNotSuicide(new DeathEventArgs()
                    {
                        limb = limb,
                        cause = cause,
                        dead = dead.Player,
                        distance = distance,
                        item = item,
                        itemName = itemName,
                        key = key,
                        killerargs = default
                    });
                }
                LogDeathMessage(key, cause, dead.Player, killerName, translateName, killerTeam, limb, itemName, distance);
            }
            OnPlayerDeathPostMessages?.Invoke(dead, cause, limb, murderer);
        }
        private void LogDeathMessage(string key, EDeathCause backupcause, Player dead, FPlayerName killerName, bool translateName, ulong killerGroup, ELimb limb, string itemName, float distance)
        {
            Chat.BroadcastDeath(key, backupcause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, translateName, killerGroup, limb, itemName, distance, out string message, true);
            L.Log(message, ConsoleColor.Cyan);
        }
        private void LogLandmineMessage(string key, Player dead, FPlayerName killerName, ulong killerGroup, ELimb limb, string landmineName, FPlayerName triggererName, ulong triggererTeam)
        {
            Chat.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, killerGroup, triggererName, triggererTeam, limb, landmineName, out string message, true);
            L.Log(message, ConsoleColor.Cyan);
        }
        internal void GetKillerInfo(out Guid item, out float distance, out FPlayerName killernames, out ulong KillerTeam, out string kitname, out ushort vehicle, EDeathCause cause, SteamPlayer killer, Player dead)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            vehicle = 0;
            if (killer == null || dead == null)
            {
                killernames = dead == null ? FPlayerName.Nil : F.GetPlayerOriginalNames(dead);
                distance = 0;
                KillerTeam = dead == null ? 0 : dead.GetTeam();
                kitname = KillerTeam == 0 ? string.Empty : (KillerTeam == 1 ? TeamManager.Team1UnarmedKit : (KillerTeam == 2 ? TeamManager.Team2UnarmedKit : string.Empty));
            }
            else
            {
                killernames = F.GetPlayerOriginalNames(killer);
                distance = Vector3.Distance(killer.player.transform.position, dead.transform.position);
                KillerTeam = killer.GetTeam();
                kitname = !KitManager.HasKit(killer, out Kit kit) ? (KillerTeam == 0 ? string.Empty : (KillerTeam == 1 ? TeamManager.Team1UnarmedKit : (KillerTeam == 2 ? TeamManager.Team2UnarmedKit : string.Empty))) : kit.Name;
                if (cause == EDeathCause.GUN || cause == EDeathCause.MISSILE || cause == EDeathCause.SPLASH)
                {
                    InteractableVehicle veh = killer.player.movement.getVehicle();
                    if (veh != null)
                    {
                        for (int p = 0; p < veh.passengers.Length; p++)
                        {
                            if (veh.passengers[p] != null && veh.passengers[p].player != null && veh.passengers[p].player.playerID.steamID.m_SteamID == killer.playerID.steamID.m_SteamID)
                            {
                                if (veh.passengers[p].turret != null)
                                    vehicle = veh.id;
                                break;
                            }
                        }
                    }
                }
            }
            if (killer != null && killer.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
            {
                if (cause == EDeathCause.GUN && c.lastShot != default)
                    item = c.lastShot;
                else if (cause == EDeathCause.GRENADE && c.thrown != default && c.thrown.Count > 0)
                {
                    ThrowableOwner g = c.thrown.FirstOrDefault(x => Assets.find(x.ThrowableID) is ItemThrowableAsset asset && asset.isExplosive);
                    if (g != default)
                    {
                        item = g.ThrowableID;
                        if (Config.Debug)
                            L.Log("Cause was grenade and found id: " + item.ToString(), ConsoleColor.DarkGray);
                    }
                    else if (c.thrown[0] != null)
                    {
                        item = c.thrown[0].ThrowableID;
                        if (Config.Debug)
                            L.Log("Cause was grenade and found id: " + item.ToString(), ConsoleColor.DarkGray);
                    }
                    else item = killer.player.equipment.asset.GUID;
                }
                else if (cause == EDeathCause.MISSILE && c.lastProjected != default)
                    item = c.lastProjected;
                else if (cause == EDeathCause.VEHICLE && c.lastExplodedVehicle != default)
                    item = c.lastExplodedVehicle;
                else if (cause == EDeathCause.ROADKILL && c.lastRoadkilled != default)
                    item = c.lastRoadkilled;
                else item = killer.player.equipment.asset.GUID;
            }
            else if (killer != null) item = killer.player.equipment.asset.GUID;
            else item = default;
        }
    }
    public class DeathInfo
    {
        public float distance;
        public Guid item;
        public FPlayerName killerName;
        public ulong killerTeam;
        public string kitName;
        public ushort vehicle;
    }
}
