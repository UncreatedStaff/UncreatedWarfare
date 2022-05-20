using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
{
    class LoadCurrentBans : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Console;
        public string Name => "loadbans";
        public string Help => "Load any current bans.";
        public string Syntax => "/loadbans";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.loadbans" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (SteamBlacklist.list.Count == 0)
                L.LogError(Translation.Translate("loadbans_NoBansErrorText", 0));
            else
            {
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    for (int index = 0; index < SteamBlacklist.list.Count; ++index)
                    {
                        SteamBlacklistID ban = SteamBlacklist.list[index];
                        DateTime time = DateTime.Now - TimeSpan.FromSeconds(ban.duration - ban.getTime());
                        Data.DatabaseManager.AddBan(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, ban.duration / 60, ban.reason, time);
                        OffenseManager.NetCalls.SendPlayerBanned.NetInvoke(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, ban.reason, ban.duration / 60, time);
                    }
                    ActionLog.Add(EActionLogType.LOAD_OLD_BANS, SteamBlacklist.list.Count + " BANS LOADED.");
                }
                else
                    L.LogError(Translation.Translate("loadbans_LogBansDisabled", 0));
            }
        }
    }
}
