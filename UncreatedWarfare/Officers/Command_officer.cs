using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Point;

namespace Uncreated.Warfare.Commands;
public class OfficerCommand : AsyncCommand
{
    private const string SYNTAX = "/officer <discharge|setrank> <player> [value] [team = current team]";
    private const string HELP = "Promotes or demotes a player to an officer rank.";

    public OfficerCommand() : base("officer", EAdminType.MODERATOR) { }

    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertOnDuty();

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);

        if (ctx.MatchParameter(0, "setrank", "set"))
        {
            ctx.AssertHelpCheck(1, "/officer set <player> <rank> [team = current team]");

            if (ctx.TryGet(2, out int level) &&
                ctx.TryGet(1, out ulong steam64, out UCPlayer? onlinePlayer))
            {
                if (!ctx.TryGetTeam(3, out ulong team))
                {
                    if (onlinePlayer is not null)
                        team = onlinePlayer.GetTeam();
                    else if (PlayerSave.TryReadSaveFile(steam64, out PlayerSave save))
                        team = save.Team;
                    else
                        throw ctx.SendPlayerNotFound();
                }

                if (team == 3)
                    throw ctx.SendPlayerNotFound();

                OfficerStorage.ChangeOfficerRank(steam64, level, team);
                if (onlinePlayer is not null)
                {
                    Ranks.RankData data = Ranks.RankManager.GetRank(onlinePlayer);
                    ctx.Reply(T.OfficerChangedRankFeedback, onlinePlayer, data, Teams.TeamManager.GetFactionSafe(team)!);
                }
                else
                {
                    PlayerNames name = await F.GetPlayerOriginalNamesAsync(steam64, token).ThenToUpdate(token);
                    ctx.Reply(T.OfficerChangedRankFeedback, name, Ranks.RankManager.GetRank(level), Teams.TeamManager.GetFactionSafe(team)!);
                }
                ctx.LogAction(EActionLogType.SET_OFFICER_RANK, steam64.ToString(Data.Locale) + " to " + level + " on team " + Teams.TeamManager.TranslateName(team, 0));
            }
            else throw ctx.SendCorrectUsage("/officer set <player> <rank> [team = current team]");
        }
        else if (ctx.MatchParameter(0, "discharge", "disc", "remove"))
        {
            ctx.AssertHelpCheck(1, "/officer discharge <player>");

            if (!ctx.HasArgs(2))
                throw ctx.SendCorrectUsage("/officer discharge <player>");

            if (ctx.TryGet(1, out ulong steam64, out UCPlayer? onlinePlayer))
            {
                OfficerStorage.DischargeOfficer(steam64);
                ctx.Reply(T.OfficerDischargedFeedback, onlinePlayer as IPlayer ?? (await F.GetPlayerOriginalNamesAsync(steam64, token).ThenToUpdate(token)));
                ctx.LogAction(EActionLogType.DISCHARGE_OFFICER, steam64.ToString(Data.Locale));
            }
            else throw ctx.SendPlayerNotFound();
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
