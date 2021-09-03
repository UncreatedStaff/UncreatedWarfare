using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
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
            //F.Log("[TEAMKILL] " + parameters.ToString(), ConsoleColor.DarkRed);
            F.Log(F.Translate("teamkilled_console_log", 0,
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
                if (Configuration.Instance.AdminLoggerSettings.LogTKs)
                    Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                        parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                        parameters.key, parameters.itemName ?? "", parameters.item, parameters.distance);
                Invocations.Shared.LogTeamkilled.NetInvoke(parameters.killer.channel.owner.playerID.steamID.m_SteamID, parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                    parameters.key, parameters.itemName, DateTime.Now);
                StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, x => x.Teamkills++, false);
                if (Data.Gamemode is TeamCTF ctf)
                {
                    ctf.GameStats.teamkills++;
                }
            }
        }
        public class KillEventArgs
        {
            public Player killer;
            public Player dead;
            public Player LandmineLinkedAssistant;
            public EDeathCause cause;
            public ushort item;
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
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            new FPlayerName() { CharacterName = "Unknown", NickName = "Unknown", PlayerName = "Unknown", Steam64 = 0 }, 0, limb, itemName, out msg, false);
                    }
                    else
                    {
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            F.GetPlayerOriginalNames(LandmineLinkedAssistant), LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                }
                else
                {
                    F.BroadcastDeath(key, cause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), false, killer.GetTeam(), limb, itemName, distance, out msg, false);
                }
                return msg;
            }
        }
        private void Kill(KillEventArgs parameters)
        {
            //F.Log("[KILL] " + parameters.ToString(), ConsoleColor.Blue);
            byte team = parameters.killer.GetTeamByte();
            if (team == 1 || team == 2)
            {
                TicketManager.OnEnemyKilled(parameters);
                Data.DatabaseManager.AddKill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team);
                bool atk = false;
                bool def = false;
                if (Data.Gamemode is TeamCTF ctf)
                {
                    if (parameters.cause == EDeathCause.GUN && (ctf.GameStats.LongestShot.Player == 0 || ctf.GameStats.LongestShot.Distance < parameters.distance))
                    {
                        ctf.GameStats.LongestShot.Player = parameters.killer.channel.owner.playerID.steamID.m_SteamID;
                        ctf.GameStats.LongestShot.Distance = parameters.distance;
                        ctf.GameStats.LongestShot.Gun = parameters.item;
                        ctf.GameStats.LongestShot.Team = team;
                    }
                    try
                    {
                        for (int f = 0; f < ctf.Rotation.Count; f++)
                        {
                            Gamemodes.Flags.Flag flag = ctf.Rotation[f];
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
                        F.LogError("Error checking defending/attacking status on kill.");
                        F.LogError(ex);
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
                if (KitManager.KitExists(parameters.kitname, out kit) && parameters.cause != EDeathCause.VEHICLE && parameters.cause != EDeathCause.ROADKILL && Assets.find(EAssetType.ITEM, parameters.item) is ItemAsset asset && asset != null)
                {
                    StatsManager.ModifyWeapon(parameters.item, kit.Name, x =>
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
            public ushort item;
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
                    F.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                else
                    F.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg, false);
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
            //F.Log("[SUICIDE] " + parameters.ToString(), ConsoleColor.Blue);
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
                if (KitManager.HasKit(parameters.dead, out Kits.Kit kit))
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
                    if (Assets.find(EAssetType.ITEM, parameters.item) is ItemAsset asset && asset != null)
                        StatsManager.ModifyWeapon(parameters.item, kit.Name, w => w.Deaths++, true);
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
            public KillEventArgs killerargs;
            public EDeathCause cause;
            public ushort item;
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
                        F.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                    }
                    else
                    {
                        if (killerargs.LandmineLinkedAssistant == default)
                        {
                            if (killerargs.killer != default)
                            {
                                FPlayerName name2 = F.GetPlayerOriginalNames(killerargs.killer);
                                ulong team2 = killerargs.killer.GetTeam();
                                F.BroadcastLandmineDeath(key, name, team, name2, team2, name2, team2, limb, itemName, out msg, false);
                            }
                            else
                            {
                                F.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg, false);
                            }
                        }
                        else
                        {
                            F.BroadcastLandmineDeath(key, name, team, F.GetPlayerOriginalNames(killerargs.killer ?? dead), (killerargs.killer ?? dead).GetTeam(),
                            F.GetPlayerOriginalNames(killerargs.LandmineLinkedAssistant), killerargs.LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                        }
                    }
                }
                else
                {
                    if (killerargs == default || killerargs.killer == default)
                    {
                        F.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg, false);
                    }
                    else
                    {
                        F.BroadcastDeath(key, cause, name, team, F.GetPlayerOriginalNames(killerargs.killer), false, killerargs.killer.GetTeam(), limb, itemName, distance, out msg, false);
                    }
                }
                return msg;
            }
        }
        public void DeathNotSuicide(DeathEventArgs parameters)
        {
            //F.Log("[DEATH] " + parameters.ToString(), ConsoleColor.Blue);
            byte team = parameters.dead.GetTeamByte();
            Data.Gamemode?.OnPlayerDeath(parameters);
            if (team == 1 || team == 2)
            {
                TicketManager.OnPlayerDeath(parameters);
                Data.DatabaseManager?.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team);
                StatsManager.ModifyTeam(team, t => t.Deaths++, false);
                if (KitManager.HasKit(parameters.dead, out Kits.Kit kit))
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
                    if (secondary != null && primary.item.id != secondary.item.id) // prevents 2 of the same gun from counting twice
                        StatsManager.ModifyWeapon(secondary.item.id, kit.Name, x => x.Deaths++, true);
                    StatsManager.ModifyKit(kit.Name, k => k.Deaths++, true);
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
            OnPlayerDeathGlobal?.Invoke(parameters);
        }
        private void OnPlayerDeath(UnturnedPlayer dead, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (cause == EDeathCause.LANDMINE)
            {
                SteamPlayer placer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                Player triggerer;
                FPlayerName placerName;
                FPlayerName triggererName;
                bool foundPlacer;
                bool foundTriggerer;
                ulong deadTeam = F.GetTeam(dead);
                ulong placerTeam;
                ulong triggererTeam;
                ushort landmineID;
                LandmineDataForPostAccess landmine;
                string landmineName;
                if (placer == null)
                {
                    placer = dead.Player.channel.owner;
                    placerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    foundPlacer = false;
                    landmineID = 0;
                    landmineName = "Unknown";
                    landmine = default;
                    placerTeam = 0;
                    triggererTeam = 0;
                    triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    triggerer = null;
                    foundTriggerer = false;
                }
                else
                {
                    placerName = F.GetPlayerOriginalNames(placer);
                    placerTeam = F.GetTeam(placer);
                    foundPlacer = true;
                    if (F.TryGetPlaytimeComponent(placer.player, out PlaytimeComponent c))
                    {
                        if (c.LastLandmineExploded.Equals(default)
                            || c.LastLandmineExploded.Equals(default) || c.LastLandmineExploded.owner == null)
                        {
                            landmine = default;
                            landmineID = 0;
                        }
                        else
                        {
                            landmine = c.LastLandmineExploded;
                            landmineID = c.LastLandmineExploded.barricadeID;
                        }
                    }
                    else
                    {
                        landmineID = 0;
                        landmine = default;
                    }
                    if (landmineID != 0)
                    {
                        if (Assets.find(EAssetType.ITEM, landmineID) is ItemAsset asset) landmineName = asset.itemName;
                        else landmineName = landmineID.ToString(Data.Locale);
                    }
                    else landmineName = "Unknown";
                    if (!landmine.Equals(default))
                    {
                        KeyValuePair<ulong, PlaytimeComponent> pt = Data.PlaytimeComponents.FirstOrDefault(
                            x =>
                            x.Value != default &&
                            !x.Value.LastLandmineTriggered.Equals(default) &&
                            x.Value.LastLandmineTriggered.owner != default &&
                            landmine.barricadeInstId == x.Value.LastLandmineTriggered.barricadeInstId);
                        if (!pt.Equals(default(KeyValuePair<ulong, PlaytimeComponent>)))
                        {
                            triggerer = pt.Value.player;
                            triggererTeam = F.GetTeam(triggerer);
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
                }
                string key = "LANDMINE";
                string itemkey = landmineID.ToString(Data.Locale);
                if (foundPlacer && placer.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                {
                    key += "_SUICIDE";
                }
                if (landmineID == 0)
                {
                    key += "_UNKNOWN";
                }
                if (foundTriggerer && triggerer.channel.owner.playerID.steamID.m_SteamID != dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID != placer.playerID.steamID.m_SteamID)
                {
                    key += "_TRIGGERED";
                }
                if (!foundPlacer)
                {
                    key += "_UNKNOWNKILLER";
                }
                if (foundPlacer && foundTriggerer)
                {
                    if (triggerer.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID == placer.playerID.steamID.m_SteamID)
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
                    if (triggerer.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
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
                SteamPlayer killer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                FPlayerName killerName;
                bool foundKiller;
                ushort item;
                string itemName = null;
                float distance = 0f;
                bool translateName = false;
                ulong killerTeam;
                ushort turretOwner = 0;
                string kitname;
                bool itemIsVehicle = cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL;
                if (killer == null)
                {
                    if (cause != EDeathCause.ZOMBIE && Data.ReviveManager.DeathInfo.TryGetValue(dead.CSteamID.m_SteamID, out DeathInfo info))
                    {
                        item = info.item;
                        distance = info.distance;
                        killerName = info.killerName;
                        killer = null;
                        killerTeam = info.killerTeam;
                        kitname = info.kitName;
                        foundKiller = false;
                        bool foundvehasset = true;
                        if (itemIsVehicle)
                        {
                            VehicleAsset asset = (VehicleAsset)Assets.find(EAssetType.VEHICLE, item);
                            if (asset != null) itemName = asset.vehicleName;
                            else
                            {
                                itemName = item.ToString(Data.Locale);
                                foundvehasset = false;
                            }
                        }
                        if (!itemIsVehicle || !foundvehasset)
                        {
                            ItemAsset asset = (ItemAsset)Assets.find(EAssetType.ITEM, item);
                            if (asset != null) itemName = asset.itemName;
                            else itemName = item.ToString(Data.Locale);
                        }

                    }
                    else
                    {
                        killer = dead.Player.channel.owner;
                        if (cause == EDeathCause.ZOMBIE)
                        {
                            killerName = new FPlayerName() { CharacterName = "zombie", PlayerName = "zombie", NickName = "zombie", Steam64 = 0 };
                            killerTeam = TeamManager.ZombieTeamID;
                            translateName = true;
                        }
                        else
                        {
                            killerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                            killerTeam = 0;
                        }
                        foundKiller = false;
                        kitname = string.Empty;
                        item = 0;
                        itemName = "Unknown";
                    }
                }
                else
                {
                    killerName = F.GetPlayerOriginalNames(killer);
                    killerTeam = F.GetTeam(killer);
                    foundKiller = true;
                    try
                    {
                        if (!Data.ReviveManager.DeathInfo.TryGetValue(dead.CSteamID.m_SteamID, out DeathInfo info))
                            GetKillerInfo(out item, out distance, out _, out _, out kitname, out turretOwner, cause, killer, dead.Player);
                        else
                        {
                            item = info.item;
                            distance = info.distance;
                            if (KitManager.HasKit(killer, out Kits.Kit kit))
                                kitname = kit.Name;
                            else kitname = killerTeam == 0 ? string.Empty : (killerTeam == 1 ? TeamManager.Team1UnarmedKit : (killerTeam == 2 ? TeamManager.Team2UnarmedKit : string.Empty));
                            turretOwner = info.vehicle;
                        }
                    }
                    catch { item = 0; kitname = string.Empty; }
                    if (item != 0)
                    {
                        if (itemIsVehicle)
                        {
                            VehicleAsset asset = (VehicleAsset)Assets.find(EAssetType.VEHICLE, item);
                            if (asset != null) itemName = asset.vehicleName;
                            else itemName = item.ToString(Data.Locale);
                        }
                        else
                        {
                            ItemAsset asset = (ItemAsset)Assets.find(EAssetType.ITEM, item);
                            if (asset != null) itemName = asset.itemName;
                            else itemName = item.ToString(Data.Locale);
                        }
                    }
                    else itemName = "Unknown";
                }
                string key = cause.ToString();
                if (dead.CSteamID.m_SteamID == murderer.m_SteamID && cause != EDeathCause.SUICIDE) key += "_SUICIDE";
                if (cause == EDeathCause.ARENA && Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey("MAINCAMP")) key = "MAINCAMP";
                else if (cause == EDeathCause.ACID && Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey("MAINDEATH")) key = "MAINDEATH";
                if ((cause == EDeathCause.GUN || cause == EDeathCause.MELEE || cause == EDeathCause.MISSILE || cause == EDeathCause.SPLASH
                    || cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL || cause == EDeathCause.BLEEDING) && foundKiller)
                {
                    if (item != 0)
                    {
                        string k1 = (itemIsVehicle ? "v" : "") + item.ToString(Data.Locale);
                        string k2 = k1 + "_SUICIDE";
                        if (Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(k1))
                        {
                            key = k1;
                        }
                        if (dead.CSteamID.m_SteamID == killer.playerID.steamID.m_SteamID && cause != EDeathCause.SUICIDE && Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(k2))
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
                    if (killer.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
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
                        if (killerTeam != F.GetTeam(dead))
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
            F.BroadcastDeath(key, backupcause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, translateName, killerGroup, limb, itemName, distance, out string message, true);
            F.Log(message, ConsoleColor.Cyan);
        }
        private void LogLandmineMessage(string key, Player dead, FPlayerName killerName, ulong killerGroup, ELimb limb, string landmineName, FPlayerName triggererName, ulong triggererTeam)
        {
            F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, killerGroup, triggererName, triggererTeam, limb, landmineName, out string message, true);
            F.Log(message, ConsoleColor.Cyan);
        }
        internal void GetKillerInfo(out ushort item, out float distance, out FPlayerName killernames, out ulong KillerTeam, out string kitname, out ushort vehicle, EDeathCause cause, SteamPlayer killer, Player dead)
        {
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
                kitname = KillerTeam == 0 ? string.Empty : (KillerTeam == 1 ? TeamManager.Team1UnarmedKit : (KillerTeam == 2 ? TeamManager.Team2UnarmedKit : string.Empty));
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
            if (killer.player.TryGetPlaytimeComponent(out PlaytimeComponent c))
            {
                if (cause == EDeathCause.GUN && c.lastShot != default)
                    item = c.lastShot;
                else if (cause == EDeathCause.GRENADE && c.thrown != default && c.thrown.Count > 0)
                {
                    ThrowableOwnerDataComponent g = c.thrown.FirstOrDefault(x => x.asset.isExplosive);
                    if (g != default)
                    {
                        item = g.asset.id;
                        if (Config.Debug)
                            F.Log("Cause was grenade and found id: " + item.ToString(), ConsoleColor.DarkGray);
                    }
                    else if (c.thrown[0] != null)
                    {
                        item = c.thrown[0].asset.id;
                        if (Config.Debug)
                            F.Log("Cause was grenade and found id: " + item.ToString(), ConsoleColor.DarkGray);
                    }
                    else item = killer.player.equipment.itemID;
                }
                else if (cause == EDeathCause.MISSILE && c.lastProjected != default)
                    item = c.lastProjected;
                else if (cause == EDeathCause.VEHICLE && c.lastExplodedVehicle != default)
                    item = c.lastExplodedVehicle;
                else if (cause == EDeathCause.ROADKILL && c.lastRoadkilled != default)
                    item = c.lastRoadkilled;
                else item = killer.player.equipment.itemID;
            }
            else item = killer.player.equipment.itemID;
        }
    }
    public class DeathInfo
    {
        public float distance;
        public ushort item;
        public FPlayerName killerName;
        public ulong killerTeam;
        public string kitName;
        public ushort vehicle;
    }
}
