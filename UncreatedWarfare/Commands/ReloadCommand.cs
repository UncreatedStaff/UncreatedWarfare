﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using Uncreated;

namespace Uncreated.Warfare.Commands
{
    public class ReloadCommand : IRocketCommand
    {
        public static event Networking.EmptyTaskDelegate OnTranslationsReloaded;
        public static event Networking.EmptyTaskDelegate OnFlagsReloaded;
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "reload";
        public string Help => "Reload certain parts of UCWarfare.";
        public string Syntax => "/reload [module]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.reload" };
        const string ConsoleName = "Console";
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == ConsoleName;
            string cmd = command[0].ToLower();
            if (command.Length == 0)
            {
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    await ReloadTranslations();
                    ReloadConfig();
                    await ReloadFlags();

                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();

                    player.Message("Reload all Uncreated Warfare components.");
                    await rtn;
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
            }
            else
            {
                if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.config") || player.HasPermission("uc.reload.all"))
                    {
                        player.Message("Reload all Uncreated Warfare config files.");
                        ReloadConfig();
                    }
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
                else if (cmd == "translations")
                {
                    if(isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                        await ReloadTranslations();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                } else if (cmd == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                        await ReloadFlags();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                } else if (cmd == "tcp")
                {
                    if (isConsole || player.HasPermission("uc.reload.tcp") || player.HasPermission("uc.reload.all"))
                        await ReloadTCPServer(isConsole ? 0 : player.CSteamID.m_SteamID, "Reload command.");
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
            }
        }
        internal static void ReloadConfig()
        {
            try
            {
                SquadManager.config.Reload();
                TicketManager.config.Reload();
                XPManager.config.Reload();
                OfficerManager.config.Reload();
                FOBManager.config.Reload();

                UCWarfare.Instance.Configuration.Load();
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading config.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadTranslations()
        {
            try
            {
                Data.LanguageAliases = JSONMethods.LoadLangAliases();
                Data.Languages = JSONMethods.LoadLanguagePreferences();
                Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
                Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
                if(OnTranslationsReloaded != null)
                    await OnTranslationsReloaded.Invoke();
            }
            catch (Exception ex)
            { 
                F.LogError("Execption when reloading translations.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadFlags()
        {
            try
            {
                await Data.FlagManager.StartNextGame();
                SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                if(OnFlagsReloaded != null)
                    await OnFlagsReloaded.Invoke();
                await rtn;
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading flags.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadTCPServer(ulong admin, string reason)
        {
            try
            {
                await Networking.Client.SendReloading(admin, reason);
                Networking.TCPClient.I?.Shutdown();
                Data.CancelTcp.Cancel();
                Data.CancelTcp.Token.WaitHandle.WaitOne();
                Data.CancelTcp = new CancellationTokenSource();
                Networking.TCPClient.I = new Networking.TCPClient(UCWarfare.Config.PlayerStatsSettings.TCPServerIP,
                    UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
                _ = Networking.TCPClient.I.Connect(Data.CancelTcp.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading flags.");
                F.LogError(ex);
            }
        }
    }
}