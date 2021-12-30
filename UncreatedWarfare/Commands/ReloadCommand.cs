﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Tickets;

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
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == "Console";
            string cmd = command.Length == 0 ? string.Empty : command[0].ToLower();
            if (command.Length == 0 || (command.Length == 1 && cmd == "all"))
            {
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

                    if (isConsole) L.Log(Translation.Translate("reload_reloaded_all", 0, out _));
                    else player.SendChat("reload_reloaded_all");
                }
                else
                    player.Player.SendChat("no_permissions");
            }
            else
            {
                if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.config") || player.HasPermission("uc.reload.all"))
                    {
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_config", 0, out _));
                        else player.SendChat("reload_reloaded_config");
                        if (command.Length > 1 && command[1].ToLower() == "all") ReloadAllConfigFiles();
                        else ReloadConfig();
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "translations" || cmd == "lang")
                {
                    if (isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadTranslations();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_lang", 0, out _));
                        else player.SendChat("reload_reloaded_lang");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadFlags();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_flags", 0, out _));
                        else player.SendChat("reload_reloaded_flags");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "gameconfig")
                {
                    if (isConsole || player.HasPermission("uc.reload.gameconfig") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadGamemodeConfig();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_gameconfig", 0, out _));
                        else player.SendChat("reload_reloaded_gameconfig");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "tcp")
                {
                    if (isConsole || player.HasPermission("uc.reload.tcp") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadTCPServer();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_tcp", 0, out _));
                        else player.SendChat("reload_reloaded_tcp");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "sql")
                {
                    if (isConsole || player.HasPermission("uc.reload.sql") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadSQLServer();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_sql", 0, out _));
                        else player.SendChat("reload_reloaded_sql");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "kits")
                {
                    if (isConsole || player.HasPermission("uc.reload.kits") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadKits();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_kits", 0, out _));
                        else player.SendChat("reload_reloaded_kits");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.kits") || player.HasPermission("uc.reload.all"))
                    {
                        ReloadAllConfigFiles();
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_config", 0, out _));
                        else player.SendChat("reload_reloaded_config");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "slots")
                {
                    if (isConsole || player.HasPermission("uc.reload.slots") || player.HasPermission("uc.reload.all"))
                    {
                        if (!UCWarfare.Config.UsePatchForPlayerCap)
                        {
                            if (isConsole) L.Log(Translation.Translate("reload_reloaded_slots_not_enabled", 0, out _, nameof(Config.UsePatchForPlayerCap)));
                            else player.SendChat("reload_reloaded_slots_not_enabled", nameof(Config.UsePatchForPlayerCap));
                            return;
                        }
                        else if (Provider.clients.Count >= 24)
                        {
                            Provider.maxPlayers = UCWarfare.Config.MaxPlayerCount;
                        }
                        if (isConsole) L.Log(Translation.Translate("reload_reloaded_slots", 0, out _));
                        else player.SendChat("reload_reloaded_slots");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
            }
        }
        internal static void ReloadConfig()
        {
            try
            {
                Gamemode.ConfigObj.Reload();
                SquadManager.config.Reload();
                TicketManager.config.Reload();
                Points.ReloadConfig();

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
            try
            {
                Data.LanguageAliases = JSONMethods.LoadLangAliases();
                Data.Languages = JSONMethods.LoadLanguagePreferences();
                Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
                Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
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
            Gamemode.ConfigObj.Reload();
            SquadManager.TempCacheEffectIDs();
            Gamemodes.Flags.TeamCTF.CTFUI.TempCacheEffectIDs();
            LeaderboardEx.TempCacheEffectIDs();
            FOBManager.TempCacheEffectIDs();
        }
        internal static void ReloadFlags()
        {
            try
            {
                Gamemode.ConfigObj.Reload();
                if (Data.Gamemode is FlagGamemode flaggm)
                {
                    flaggm.LoadAllFlags();
                    flaggm.StartNextGame(false);
                }
                Data.ExtraZones = JSONMethods.LoadExtraZones();
                Data.ExtraPoints = JSONMethods.LoadExtraPoints();
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
            Kits.KitManager.Reload();
            foreach (Kits.RequestSign sign in Kits.RequestSigns.ActiveObjects)
            {
                sign.InvokeUpdate();
            }
        }
        internal static void ReloadAllConfigFiles()
        {
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
            Data.ReloadTCP();
        }
        internal static void ReloadSQLServer()
        {
            Data.DatabaseManager.Close();
            Data.DatabaseManager.Open();
        }
    }
}