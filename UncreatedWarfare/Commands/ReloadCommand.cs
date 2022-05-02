using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands
{
    public class ReloadCommand : IRocketCommand
    {
        public static event VoidDelegate OnTranslationsReloaded;
        public static event VoidDelegate OnFlagsReloaded;
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "reload";
        public string Help => "Reload certain parts of UCWarfare.";
        public string Syntax => "/reload [module]";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.reload" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            bool isConsole = player == null;
            ulong s64 = isConsole ? 0 : player!.Steam64;
            string cmd = command.Length == 0 ? string.Empty : command[0].ToLower();
            if (command.Length == 0 || (command.Length == 1 && cmd == "all"))
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    ReloadTranslations();
                    ReloadAllConfigFiles();
                    ReloadGamemodeConfig();
                    ReloadConfig();
                    ReloadKits();
                    ReloadFlags();
                    ReloadTCPServer();
                    ReloadSQLServer();
                    ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "ALL COMPONENTS", s64);
                    if (isConsole) L.Log(Translation.Translate("reload_reloaded_all", 0, out _));
                    else player!.SendChat("reload_reloaded_all");
                }
                else
                    player!.Player.SendChat("no_permissions");
            }
            else
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking(cmd + "_Execute");
#endif
                if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.config") || player.HasPermission("uc.reload.all"))
                    {
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_config", 0, out _));
                        else player!.SendChat("reload_reloaded_config");
                        if (command.Length > 1 && command[1].ToLower() == "all") ReloadAllConfigFiles();
                        else ReloadConfig();
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "ALL CONFIG FILES", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "translations" || cmd == "lang")
                {
                    if (isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadTranslations();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_lang", 0, out _));
                        else player!.SendChat("reload_reloaded_lang");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "TRANSLATIONS", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadFlags();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_flags", 0, out _));
                        else player!.SendChat("reload_reloaded_flags");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "FLAGS", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "gameconfig")
                {
                    if (isConsole || player.HasPermission("uc.reload.gameconfig") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadGamemodeConfig();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_gameconfig", 0, out _));
                        else player!.SendChat("reload_reloaded_gameconfig");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "GAMEMODE CONFIG FILE", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "tcp")
                {
                    if (isConsole || player.HasPermission("uc.reload.tcp") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadTCPServer();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_tcp", 0, out _));
                        else player!.SendChat("reload_reloaded_tcp");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "UC DISCORD TCP CONNECTION", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "sql")
                {
                    if (isConsole || player.HasPermission("uc.reload.sql") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadSQLServer();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_sql", 0, out _));
                        else player!.SendChat("reload_reloaded_sql");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "MYSQL CONNECTION", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
                else if (cmd == "kits")
                {
                    if (isConsole || player.HasPermission("uc.reload.kits") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadKits();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_kits", 0, out _));
                        else player!.SendChat("reload_reloaded_kits");
                        ActionLog.Add(EActionLogType.RELOAD_COMPONENT, "KITS FILE", s64);
                    }
                    else
                        player!.Player.SendChat("no_permissions");
                }
            }
        }
        internal static void ReloadConfig()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                Gamemode.ConfigObj.Reload();
                SquadManager.config.Reload();
                TicketManager.config.Reload();
                Points.ReloadConfig();
                VehicleBay.Reload();
                foreach (var data in VehicleBay.ActiveObjects)
                {
                    if (Assets.find(data.VehicleID) is VehicleAsset va)
                    {
                        data.Name = va.vehicleName;
                    }
                }
                VehicleBay.Save();

                // FIX: Invocations
                //Invocations.Warfare.SendRankInfo.NetInvoke(XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks, OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
                FOBManager.config.Reload();

                UCWarfare.Instance.Configuration.Load();
                if (Data.DatabaseManager != null) Data.DatabaseManager.DebugLogging = UCWarfare.Config.Debug;
            }
            catch (Exception ex)
            {
                L.LogError("Execption when reloading config.");
                L.LogError(ex);
            }
        }
        internal static void ReloadTranslations()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                Data.LanguageAliases = JSONMethods.LoadLangAliases();
                Data.Languages = JSONMethods.LoadLanguagePreferences();
                Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
                Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
                Translation.ReadEnumTranslations();
                if (OnTranslationsReloaded != null)
                    OnTranslationsReloaded.Invoke();
            }
            catch (Exception ex)
            {
                L.LogError("Execption when reloading translations.");
                L.LogError(ex);
            }
        }
        internal static void ReloadGamemodeConfig()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Gamemode.ConfigObj.Reload();
            SquadManager.TempCacheEffectIDs();
            Gamemodes.Flags.TeamCTF.CTFUI.TempCacheEffectIDs();
            LeaderboardEx.TempCacheEffectIDs();
            FOBManager.TempCacheEffectIDs();
            JoinManager.CacheIDs();
        }
        internal static void ReloadFlags()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                Gamemode.ConfigObj.Reload();
                if (Data.Gamemode is FlagGamemode flaggm)
                    flaggm.LoadAllFlags();
                else
                    Data.ZoneProvider.Reload();
                Data.ExtraPoints = JSONMethods.LoadExtraPoints();
                TeamManager.OnReloadFlags();
                if (OnFlagsReloaded != null)
                    OnFlagsReloaded.Invoke();
            }
            catch (Exception ex)
            {
                L.LogError("Execption when reloading flags.");
                L.LogError(ex);
            }
        }
        internal static void ReloadKits()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Task.Run(async () =>
            {
                await KitManager.Instance.Reload();
                await UCWarfare.ToUpdate();
                foreach (RequestSign sign in RequestSigns.ActiveObjects)
                {
                    sign.InvokeUpdate();
                }
                if (!KitManager.KitExists(TeamManager.Team1UnarmedKit, out _))
                    L.LogError("Team 1's unarmed kit, \"" + TeamManager.Team1UnarmedKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
                if (!KitManager.KitExists(TeamManager.Team2UnarmedKit, out _))
                    L.LogError("Team 2's unarmed kit, \"" + TeamManager.Team2UnarmedKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
                if (!KitManager.KitExists(TeamManager.DefaultKit, out _))
                    L.LogError("The default kit, \"" + TeamManager.DefaultKit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            });
        }
        internal static void ReloadAllConfigFiles()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                UCWarfare.I.Announcer.Reload();
                IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
                foreach (FieldInfo obj in objects)
                {
                    try
                    {
                        object o = obj.GetValue(null);
                        IEnumerable<FieldInfo> configfields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).
                            Where(x => x.FieldType.GetInterfaces().Contains(typeof(IConfiguration)));
                        foreach (FieldInfo config in configfields)
                        {
                            IConfiguration c;
                            if (config.IsStatic)
                            {
                                c = (IConfiguration)config.GetValue(null);
                            }
                            else
                            {
                                c = (IConfiguration)config.GetValue(o);
                            }
                            c.Reload();
                        }
                    }
                    catch (Exception) { }
                }
                // FIX: Invocations
                //Invocations.Warfare.SendRankInfo.NetInvoke(XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks, OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
            }
            catch (Exception ex)
            {
                L.LogError("Failed to find all objects in type " + typeof(Data).Name);
                L.LogError(ex);
            }
        }
        internal static void ReloadTCPServer()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Data.ReloadTCP();
        }
        internal static void ReloadSQLServer()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Data.DatabaseManager.Close();
            Data.DatabaseManager.Open();
        }
    }
}