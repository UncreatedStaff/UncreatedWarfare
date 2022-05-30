using Rocket.Unturned.Enumerations;
using SDG.Unturned;
using System;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare;

partial class UCWarfare
{
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
            Task.Run(() =>
                Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team));
            StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, s => s.Teamkills++,
                false);
            StatsManager.ModifyTeam(team, t => t.Teamkills++, false);
            if (parameters.killer.TryGetPlayerData(out UCPlayerData c) && c.stats is ITeamPVPModeStats tpvp)
                tpvp.AddTeamkill();
            if (Configuration.Instance.AdminLoggerSettings.LogTKs)
            {
                Asset a = Assets.find(parameters.item);
                Data.DatabaseManager.AddTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                    parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                    parameters.key, parameters.itemName, a == null ? (ushort)0 : a.id, parameters.distance);
            }

            OffenseManager.NetCalls.SendTeamkill.NetInvoke(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                parameters.dead.channel.owner.playerID.steamID.m_SteamID,
                parameters.key, parameters.itemName, DateTime.Now);
            StatsManager.ModifyStats(parameters.killer.channel.owner.playerID.steamID.m_SteamID, x => x.Teamkills++,
                false);
            Data.Reporter?.OnTeamkill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, parameters.item,
                parameters.dead.channel.owner.playerID.steamID.m_SteamID, parameters.cause);
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
                    Chat.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(),
                        F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                        new FPlayerName()
                            { CharacterName = "Unknown", NickName = "Unknown", PlayerName = "Unknown", Steam64 = 0 }, 0,
                        limb, itemName, out msg, false);
                }
                else
                {
                    Chat.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(),
                        F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                        F.GetPlayerOriginalNames(LandmineLinkedAssistant), LandmineLinkedAssistant.GetTeam(), limb,
                        itemName, out msg, false);
                }
            }
            else
            {
                Chat.BroadcastDeath(key, cause, F.GetPlayerOriginalNames(dead), dead.GetTeam(),
                    F.GetPlayerOriginalNames(killer), false, killer.GetTeam(), limb, itemName, distance, out msg,
                    false);
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
            Task.Run(() =>
                Data.DatabaseManager.AddKill(parameters.killer.channel.owner.playerID.steamID.m_SteamID, team));
            if (parameters.killer.TryGetPlayerData(out UCPlayerData c))
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
                if (ws.GameStats != null && parameters.cause == EDeathCause.GUN &&
                    (ls.LongestShot.Player == 0 || ls.LongestShot.Distance < parameters.distance))
                {
                    ls.LongestShot = new LongestShot(parameters.killer.channel.owner.playerID.steamID.m_SteamID,
                        parameters.distance, parameters.item, team);
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
                            (d.Cache.Structure.model.transform.position - parameters.killer.transform.position)
                            .sqrMagnitude <=
                            Gamemode.ConfigObj.Data.Insurgency.CacheDiscoverRange *
                            Gamemode.ConfigObj.Data.Insurgency.CacheDiscoverRange)
                        {
                            if (parameters.killer.TryGetPlayerData(out UCPlayerData comp) &&
                                comp.stats is InsurgencyPlayerStats ps) ps._killsDefense++;
                        }
                    }
                }
                else if (team == ins.AttackingTeam && parameters.dead != null)
                {
                    for (int i = 0; i < ins.Caches.Count; i++)
                    {
                        Insurgency.CacheData d = ins.Caches[i];
                        if (d.IsActive && !d.IsDestroyed && d.Cache != null && d.Cache.Structure != null &&
                            (d.Cache.Structure.model.transform.position - parameters.dead.transform.position)
                            .sqrMagnitude <=
                            Gamemode.ConfigObj.Data.Insurgency.CacheDiscoverRange *
                            Gamemode.ConfigObj.Data.Insurgency.CacheDiscoverRange)
                        {
                            if (parameters.killer.TryGetPlayerData(out UCPlayerData comp) &&
                                comp.stats is InsurgencyPlayerStats ps) ps._killsAttack++;
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
            if (parameters.turretOwner != 0 &&
                Assets.find(EAssetType.VEHICLE, parameters.turretOwner) is VehicleAsset vasset && vasset != null)
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
                            kitData.AverageGunKillDistance =
                                (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter +
                                 parameters.distance) / ++kitData.AverageGunKillDistanceCounter;
                        s.Kits.Add(kitData);
                    }
                    else
                    {
                        kitData.Kills++;
                        if (parameters.cause == EDeathCause.GUN)
                            kitData.AverageGunKillDistance =
                                (kitData.AverageGunKillDistance * kitData.AverageGunKillDistanceCounter +
                                 parameters.distance) / ++kitData.AverageGunKillDistanceCounter;
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
                {
                    s.Kills++;
                    if (atk) s.KillsWhileAttackingFlags++;
                    else if (def) s.KillsWhileDefendingFlags++;
                }, false);

            if (KitManager.KitExists(parameters.kitname, out kit) && parameters.cause != EDeathCause.VEHICLE &&
                parameters.cause != EDeathCause.ROADKILL && Assets.find(parameters.item) is ItemAsset asset)
            {
                StatsManager.ModifyWeapon(asset.id, kit.Name, x =>
                {
                    x.Kills++;
                    if (parameters.limb == ELimb.SKULL)
                        x.SkullKills++;
                    else if (parameters.limb == ELimb.SPINE || parameters.limb == ELimb.LEFT_FRONT ||
                             parameters.limb == ELimb.RIGHT_FRONT ||
                             parameters.limb == ELimb.LEFT_BACK || parameters.limb == ELimb.RIGHT_BACK)
                        x.BodyKills++;
                    else if (parameters.limb == ELimb.LEFT_HAND || parameters.limb == ELimb.RIGHT_HAND ||
                             parameters.limb == ELimb.LEFT_ARM || parameters.limb == ELimb.RIGHT_ARM)
                        x.ArmKills++;
                    else if (parameters.limb == ELimb.LEFT_FOOT || parameters.limb == ELimb.RIGHT_FOOT ||
                             parameters.limb == ELimb.LEFT_LEG || parameters.limb == ELimb.RIGHT_LEG)
                        x.LegKills++;
                    x.AverageKillDistance =
                        (x.AverageKillDistance * x.AverageKillDistanceCounter + parameters.distance) /
                        ++x.AverageKillDistanceCounter;
                }, true);
                StatsManager.ModifyKit(kit.Name, k =>
                {
                    k.Kills++;
                    if (parameters.cause == EDeathCause.GUN)
                        k.AverageGunKillDistance =
                            (k.AverageGunKillDistance * k.AverageGunKillDistanceCounter + parameters.distance) /
                            ++k.AverageGunKillDistanceCounter;
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
                Chat.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg,
                    false);
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
        //OnPlayerDeathGlobal?.Invoke(args);
        Data.Gamemode?.OnPlayerDeath(args);

        ActionLog.Add(EActionLogType.DEATH, args.ToStringIDs(),
            parameters.dead.channel.owner.playerID.steamID.m_SteamID);

        byte team = parameters.dead.GetTeamByte();
        if (team == 1 || team == 2)
        {
            Task.Run(
                () => Data.DatabaseManager.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team));
            StatsManager.ModifyTeam(team, t => t.Deaths++, false);
            QuestManager.OnDeath(parameters);
            if (parameters.dead.TryGetPlayerData(out UCPlayerData c) && c.stats is IPVPModeStats kd)
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
                StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s => s.Deaths++,
                    false);

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
                    Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg,
                        false);
                }
                else
                {
                    if (killerargs.LandmineLinkedAssistant == default)
                    {
                        if (killerargs.killer != default)
                        {
                            FPlayerName name2 = F.GetPlayerOriginalNames(killerargs.killer);
                            ulong team2 = killerargs.killer.GetTeam();
                            Chat.BroadcastLandmineDeath(key, name, team, name2, team2, name2, team2, limb, itemName,
                                out msg, false);
                        }
                        else
                        {
                            Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName,
                                out msg, false);
                        }
                    }
                    else
                    {
                        Chat.BroadcastLandmineDeath(key, name, team,
                            F.GetPlayerOriginalNames(killerargs.killer ?? dead), (killerargs.killer ?? dead).GetTeam(),
                            F.GetPlayerOriginalNames(killerargs.LandmineLinkedAssistant),
                            killerargs.LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                }
            }
            else
            {
                if (killerargs == default || killerargs.killer == default)
                {
                    Chat.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg,
                        false);
                }
                else
                {
                    Chat.BroadcastDeath(key, cause, name, team, F.GetPlayerOriginalNames(killerargs.killer), false,
                        killerargs.killer.GetTeam(), limb, itemName, distance, out msg, false);
                }
            }

            return msg;
        }

        public string ToStringIDs()
        {
            string msg;
            FPlayerName name = new FPlayerName(dead.channel.owner.playerID.steamID.m_SteamID);
            ulong team = dead.GetTeam();
            if (cause == EDeathCause.LANDMINE)
            {
                if (killerargs == default)
                {
                    Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName, out msg,
                        false);
                }
                else
                {
                    if (killerargs.LandmineLinkedAssistant == default)
                    {
                        if (killerargs.killer != default)
                        {
                            FPlayerName name2 =
                                new FPlayerName(killerargs.killer.channel.owner.playerID.steamID.m_SteamID);
                            ulong team2 = killerargs.killer.GetTeam();
                            Chat.BroadcastLandmineDeath(key, name, team, name2, team2, name2, team2, limb, itemName,
                                out msg, false);
                        }
                        else
                        {
                            Chat.BroadcastLandmineDeath(key, name, team, name, team, name, team, limb, itemName,
                                out msg, false);
                        }
                    }
                    else if (killerargs.killer != default)
                    {
                        FPlayerName name2 = new FPlayerName(killerargs.killer.channel.owner.playerID.steamID.m_SteamID);
                        FPlayerName name3 = new FPlayerName(killerargs.LandmineLinkedAssistant.channel.owner.playerID
                            .steamID.m_SteamID);
                        Chat.BroadcastLandmineDeath(key, name, team, name2, killerargs.killer.GetTeam(),
                            name3, killerargs.LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                    else
                    {
                        FPlayerName name3 = new FPlayerName(killerargs.LandmineLinkedAssistant.channel.owner.playerID
                            .steamID.m_SteamID);
                        Chat.BroadcastLandmineDeath(key, name, team, name, team, name3,
                            killerargs.LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                }
            }
            else
            {
                if (killerargs == default || killerargs.killer == default)
                {
                    Chat.BroadcastDeath(key, cause, name, team, name, false, team, limb, itemName, distance, out msg,
                        false);
                }
                else
                {
                    FPlayerName name2 = new FPlayerName(killerargs.killer.channel.owner.playerID.steamID.m_SteamID);
                    Chat.BroadcastDeath(key, cause, name, team, name2, false, killerargs.killer.GetTeam(), limb,
                        itemName, distance, out msg, false);
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
        bool isTeamkill = parameters.killerargs != null && parameters.killerargs.killer != null &&
                          parameters.killerargs.teamkill;
        ActionLog.Add(EActionLogType.DEATH,
            isTeamkill ? "TEAMKILLED: " + parameters.ToStringIDs() : parameters.ToStringIDs(),
            parameters.dead.channel.owner.playerID.steamID.m_SteamID);
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

            if (parameters.dead.TryGetPlayerData(out UCPlayerData c) && c.stats is IPVPModeStats kd)
                kd.AddDeath();
            Task.Run(
                () => Data.DatabaseManager.AddDeath(parameters.dead.channel.owner.playerID.steamID.m_SteamID, team));
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
                ItemJar secondary = parameters.dead.inventory.items[(int)InventoryGroup.Secondary].items
                    .FirstOrDefault();
                if (primary != null)
                    StatsManager.ModifyWeapon(primary.item.id, kit.Name, x => x.Deaths++, true);
                if (secondary != null &&
                    (primary == null ||
                     primary.item.id != secondary.item.id)) // prevents 2 of the same gun from counting twice
                    StatsManager.ModifyWeapon(secondary.item.id, kit.Name, x => x.Deaths++, true);
                StatsManager.ModifyKit(kit.Name, k => k.Deaths++, true);
            }
            else
                StatsManager.ModifyStats(parameters.dead.channel.owner.playerID.steamID.m_SteamID, s => s.Deaths++,
                    false);
        }
        //OnPlayerDeathGlobal?.Invoke(parameters);
    }
}