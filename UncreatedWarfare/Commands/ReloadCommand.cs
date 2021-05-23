﻿using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
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
            if (command.Length == 0)
            {
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    ReloadTranslations();
                    ReloadFlags();
                }
                else
                    player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
            } else
            {
                if (command[0] == "translations")
                {
                    if(isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                        ReloadTranslations();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                } else if (command[0] == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                        ReloadFlags();
                    else
                        player.Player.SendChat("no_permissions", UCWarfare.GetColor("no_permissions"));
                }
            }
        }
        void ReloadTranslations()
        {
            Data.LanguageAliases = JSONMethods.LoadLangAliases();
            Data.Languages = JSONMethods.LoadLanguagePreferences();
            Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
            Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
            OnTranslationsReloaded?.Invoke(this, EventArgs.Empty);
        }
        void ReloadFlags()
        {
            Data.FlagManager.StartNextGame();
            OnFlagsReloaded?.Invoke(this, EventArgs.Empty);
        }
    }
}