using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == ConsoleName;
            string cmd = command[0].ToLower();
            if (command.Length == 0)
            {
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    ReloadTranslations();
                    await ReloadFlags();
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
            } else
            {
                if (cmd == "translations")
                {
                    if(isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                        ReloadTranslations();
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
        internal static void ReloadTranslations()
        {
            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            OnTranslationsReloaded?.Invoke(null, EventArgs.Empty);
        }
        internal static async Task ReloadFlags()
        {
            await Data.FlagManager.StartNextGame();
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            OnFlagsReloaded?.Invoke(null, EventArgs.Empty);
            await rtn;
        }
        internal static async Task ReloadTCPServer(ulong admin, string reason)
        {
            await Networking.Client.SendReloading(admin, reason);
            Networking.TCPClient.I?.Shutdown();
            Networking.TCPClient.I = null;
        }
    }
}