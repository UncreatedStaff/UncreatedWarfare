using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class LoadOldBansCommand : Command
{
    private const string SYNTAX = "/loadbans";
    private const string HELP = "Load any current bans.";

    public LoadOldBansCommand() : base("loadbans", EAdminType.VANILLA_ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertRanByConsole();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (SteamBlacklist.list.Count == 0)
            throw ctx.Reply("loadbans_NoBansErrorText");

        for (int index = 0; index < SteamBlacklist.list.Count; ++index)
        {
            SteamBlacklistID ban = SteamBlacklist.list[index];
            DateTime time = DateTime.Now - TimeSpan.FromSeconds(ban.duration - ban.getTime());
            int duration = ban.duration == SteamBlacklist.PERMANENT ? -1 : (int)(ban.duration / 60);
            Data.DatabaseManager.AddBan(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, duration, ban.reason, time);
            OffenseManager.NetCalls.SendPlayerBanned.NetInvoke(ban.playerID.m_SteamID, ban.judgeID.m_SteamID, ban.reason, duration, time);
        }
        ctx.LogAction(EActionLogType.LOAD_OLD_BANS, SteamBlacklist.list.Count + " BANS LOADED.");

        ctx.Defer();
    }
}
