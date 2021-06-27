using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;

namespace Uncreated.Warfare.Commands
{
    class LoadCurrentBans : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Console;
        public string Name => "loadbans";
        public string Help => "Load any current bans.";
        public string Syntax => "/loadbans";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.loadbans" };
        public async void Execute(IRocketPlayer caller, string[] command)
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
                        await Client.LogPlayerBanned(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, ban.reason, ban.duration / 60, DateTime.Now - TimeSpan.FromSeconds(ban.duration - ban.getTime()));
                    }
                }
                else
                    F.LogError(F.Translate("loadbans_LogBansDisabled", 0));
            }
        }
    }
}
