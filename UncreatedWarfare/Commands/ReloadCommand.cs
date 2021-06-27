using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;

namespace Uncreated.Warfare.Commands
{
    public class ReloadCommand : IRocketCommand
    {
        public static event EventHandler OnTranslationsReloaded;
        public static event EventHandler OnFlagsReloaded;
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "reload";
        public string Help => "Reload certain parts of UCWarfare.";
        public string Syntax => "/reload [module]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.reload" };
        const string ConsoleName = "Console";
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == ConsoleName;
            string cmd = command[0].ToLower();
            if (command.Length == 0)
            {
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    ReloadTranslations();
                    ReloadFlags();
                    ReloadConfig();

                    player.Message("Reload all Uncreated Warfare components.");
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
            }
            else
            {
                if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
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
                        ReloadTranslations();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                } else if (cmd == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                        ReloadFlags();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                } else if (cmd == "tcp")
                {
                    if (isConsole || player.HasPermission("uc.reload.tcp") || player.HasPermission("uc.reload.all"))
                        ReloadTCPServer();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
            }
        }
        internal static void ReloadConfig()
        {
            SquadManager.config.Reload();
            TicketManager.config.Reload();
            XPManager.config.Reload();
            OfficerManager.config.Reload();
            FOBManager.config.Reload();

            UCWarfare.Instance.Configuration.Load();
        }
        internal static void ReloadTranslations()
        {
            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            OnTranslationsReloaded?.Invoke(null, EventArgs.Empty);
        }
        internal static void ReloadFlags()
        {
            Data.FlagManager.StartNextGame();
            OnFlagsReloaded?.Invoke(null, EventArgs.Empty);
        }
        internal static void ReloadTCPServer()
        {
            Networking.Client.SendReloading();
            Networking.TCPClient.I?.Shutdown();
            Networking.TCPClient.I = null;
        }
    }
}