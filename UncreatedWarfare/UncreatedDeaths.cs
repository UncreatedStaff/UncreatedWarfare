﻿using Rocket.Core.Steam;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare
{
    partial class UCWarfare
    {
        public delegate void PlayerDeathHandler(DeathEventArgs death);
        public static event PlayerDeathHandler OnPlayerDeathGlobal;
        public event Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerDeath OnPlayerDeathPostMessages;
        public event EventHandler<KillEventArgs> OnTeamkill;
        private void Teamkill(KillEventArgs parameters)
        {
            F.Log("[TEAMKILL] " + parameters.ToString(), ConsoleColor.DarkRed);
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
                if (Configuration.Instance.AdminLoggerSettings.LogTKs)
                    Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                        parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                        parameters.key, parameters.itemName ?? "", parameters.item, parameters.distance);
                if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent pt))
                {
                    pt.stats.AddTeamkill();
                    pt.UCPlayerStats.warfare_stats.TellTeamkill(parameters, false);
                    pt.UCPlayerStats.Save();
                }
                if (Data.Gamemode is TeamCTF ctf)
                {
                    ctf.GameStats.teamkills++;
                }
            }
            OnTeamkill?.Invoke(this, parameters);
        }
        public class KillEventArgs : EventArgs
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
        public event EventHandler<KillEventArgs> OnKill;
        private void Kill(KillEventArgs parameters)
        {
            F.Log("[KILL] " + parameters.ToString(), ConsoleColor.Blue);
            byte team = parameters.killer.GetTeamByte();
            if (team == 1 || team == 2)
            {
                TicketManager.OnEnemyKilled(parameters);
                Data.DatabaseManager.AddKill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team);
                if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent pt))
                {
                    pt.stats.AddKill();
                    pt.UCPlayerStats.warfare_stats.TellKill(parameters, false);
                    pt.UCPlayerStats.Save();
                }
            }
            OnKill?.Invoke(this, parameters);
        }
        public class SuicideEventArgs : EventArgs
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
        public event EventHandler<SuicideEventArgs> OnSuicide;
        public void Suicide(SuicideEventArgs parameters)
        {
            F.Log("[SUICIDE] " + parameters.ToString(), ConsoleColor.Blue);
            OnSuicide?.Invoke(this, parameters);
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
                if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent pt))
                {
                    pt.stats.AddDeath();
                    pt.UCPlayerStats.warfare_stats.TellDeathSuicide(parameters, false);
                    pt.UCPlayerStats.Save();
                }
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
        public class DeathEventArgs : EventArgs
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
        public event EventHandler<DeathEventArgs> OnDeathNotSuicide;
        public void DeathNotSuicide(DeathEventArgs parameters)
        {
            F.Log("[DEATH] " + parameters.ToString(), ConsoleColor.Blue);
            byte team = parameters.dead.GetTeamByte();
            Data.Gamemode?.OnPlayerDeath(parameters);
            if (team == 1 || team == 2)
            {
                TicketManager.OnPlayerDeath(parameters);
                Data.DatabaseManager?.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team);
                if (parameters.dead.TryGetPlaytimeComponent(out PlaytimeComponent pt))
                {
                    pt.stats.AddDeath();

                    pt.UCPlayerStats.warfare_stats.TellDeathNonSuicide(parameters, false);
                    pt.UCPlayerStats.Save();
                }
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
            OnDeathNotSuicide?.Invoke(this, parameters);
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
                        ItemAsset asset = Assets.find(EAssetType.ITEM, landmineID) as ItemAsset;
                        if (asset != null) landmineName = asset.itemName;
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
                                teamkill = true
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
                                teamkill = false
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
                                teamkill = false
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
                                teamkill = false
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
                            teamkill = true
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
                            teamkill = false
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
                            teamkill = true
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
                            teamkill = false
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
                        
                    } else
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
                            GetKillerInfo(out item, out distance, out _, out _, cause, killer, dead.Player);
                        else
                        {
                            item = info.item;
                            distance = info.distance;
                        }
                    }
                    catch { item = 0; }
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
                                teamkill = false
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
                                teamkill = true
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
        internal void GetKillerInfo(out ushort item, out float distance, out FPlayerName killernames, out ulong KillerTeam, EDeathCause cause, SteamPlayer killer, Player dead)
        {
            if (killer == null || dead == null)
            {
                killernames = dead == null ? FPlayerName.Nil : F.GetPlayerOriginalNames(dead);
                distance = 0;
                KillerTeam = dead == null ? 0 : dead.GetTeam();
            }
            else
            {
                killernames = F.GetPlayerOriginalNames(killer);
                distance = Vector3.Distance(killer.player.transform.position, dead.transform.position);
                KillerTeam = killer.GetTeam();
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
                    } else if (c.thrown[0] != null)
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
    }
}
