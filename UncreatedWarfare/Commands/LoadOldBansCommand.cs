﻿using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Commands
{
    class LoadCurrentBans : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Console;
        public string Name => "loadbans";
        public string Help => "Load any current bans.";
        public string Syntax => "/loadbans";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.loadbans" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!Dedicator.isDedicated)
                return;
            if (SteamBlacklist.list.Count == 0)
                F.LogError(F.Translate("loadbans_NoBansErrorText", 0));
            else
            {
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    for (int index = 0; index < SteamBlacklist.list.Count; ++index)
                    {
                        SteamBlacklistID ban = SteamBlacklist.list[index];
                        bool last = index >= SteamBlacklist.list.Count - 1;
                        Data.DatabaseManager.GetUsernameAsync(ban.playerID.m_SteamID, (playernames, playersuccess) =>
                        {
                            Data.DatabaseManager.GetUsernameAsync(ban.judgeID.m_SteamID, (judgenames, judgesuccess) =>
                            {
                                Data.WebInterface?.LogBan(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, playernames.PlayerName, judgenames.PlayerName, 0, ban.reason, ban.duration / 60, DateTime.Now - TimeSpan.FromSeconds(ban.duration - ban.getTime()));
                                if(last)
                                    F.Log(F.Translate("loadbans_UploadedBans", 0, SteamBlacklist.list.Count, SteamBlacklist.list.Count.S()));
                            });
                        });
                    }
                }
                else
                    F.LogError(F.Translate("loadbans_LogBansDisabled", 0));
            }
        }
    }
}