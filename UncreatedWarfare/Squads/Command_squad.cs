using Rocket.API;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Squads;

public class SquadCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>() { "uc.squad" };
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "squad";
    public string Help => "Creates or disbands a squad";
    public string Syntax => "/squad <create|join|(un)lock|kick|leave|disband|promote> [parameters...]";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (!ctx.IsConsoleReply() || !ctx.CheckGamemodeAndSend<ISquads>()) return;
        if (!UCWarfare.Config.EnableSquads || !SquadManager.Loaded)
        {
            ctx.Reply("squads_disabled");
            return;
        }
        if (!ctx.HasArg(0))
        {
            ctx.SendCorrectUsage(Syntax);
            return;
        }
        ulong team = ctx.Caller!.GetTeam();
        if (team is < 1 or > 2)
        {
            ctx.Reply("squad_not_in_team");
            return;
        }
        if (ctx.MatchParameter(0, "create"))
        {
            if (ctx.Caller!.Squad is not null)
                ctx.Reply("squad_e_insquad");
            else if (SquadManager.Squads.Count(x => x.Team == team) >= SquadManager.ListUI.Squads.Length)
                ctx.Reply("squad_too_many");
            else
            {
                Squad squad = SquadManager.CreateSquad(ctx.Caller, team);
                ctx.Reply("squad_created", squad.Name);
            }
        }
        else if (ctx.MatchParameter(0, "join"))
        {
            if (!ctx.TryGetRange(1, out string name))
                ctx.SendCorrectUsage("/squad join <squad name or first letter>");
            else if (ctx.Caller!.Squad is not null)
                ctx.Reply("squad_e_insquad");
            else if (SquadManager.FindSquad(name, team, out Squad squad))
            {
                if (squad.IsLocked && squad.Leader.SteamPlayer.playerID.group.m_SteamID != ctx.Caller.SteamPlayer.playerID.group.m_SteamID)
                    // lets the player join if they were originally a part of the same group
                    ctx.Reply("squad_e_locked");
                else if (squad.IsFull())
                    ctx.Reply("squad_e_full");
                else
                    SquadManager.JoinSquad(ctx.Caller!, squad);
            }
            else
                ctx.Reply("squad_e_noexist", name);
        }
        else if (ctx.MatchParameter(0, "promote"))
        {
            if (!ctx.HasArg(1))
                ctx.SendCorrectUsage("/squad promote <member name>");
            else if (ctx.Caller!.Squad is null || ctx.Caller!.Squad.Leader.Steam64 != ctx.CallerID)
                ctx.Reply("squad_e_notsquadleader");
            else if (ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller!.Squad.Members))
            {
                if (playerId == ctx.CallerID)
                    ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);
                else
                    SquadManager.PromoteToLeader(ctx.Caller!.Squad, member);
            }
            else
                ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);
        }
        else if (ctx.MatchParameter(0, "kick"))
        {
            if (!ctx.HasArg(1))
                ctx.SendCorrectUsage("/squad kick <member name>");
            else if (ctx.Caller!.Squad is null || ctx.Caller!.Squad.Leader.Steam64 != ctx.CallerID)
                ctx.Reply("squad_e_notsquadleader");
            else if (ctx.TryGet(1, out ulong playerId, out UCPlayer member, ctx.Caller!.Squad.Members))
            {
                if (playerId == ctx.CallerID)
                    ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);
                else
                    SquadManager.KickPlayerFromSquad(member, ctx.Caller!.Squad);
            }
            else
                ctx.Reply("squad_e_playernotfound", ctx.Get(1)!);
        }
        else if (ctx.MatchParameter(0, "leave"))
        {
            if (ctx.Caller!.Squad is null)
                ctx.Reply("squad_e_notinsquad");
            else
                SquadManager.LeaveSquad(ctx.Caller!, ctx.Caller!.Squad);
        }
        else if (ctx.MatchParameter(0, "disband"))
        {
            if (ctx.Caller!.Squad is null || ctx.Caller!.Squad.Leader.Steam64 != ctx.CallerID)
                ctx.Reply("squad_e_notsquadleader");
            else
                SquadManager.DisbandSquad(ctx.Caller!.Squad);
        }
        else if (ctx.MatchParameter(0, "lock"))
        {
            if (ctx.Caller!.Squad is null || ctx.Caller!.Squad.Leader.Steam64 != ctx.CallerID)
                ctx.Reply("squad_e_notsquadleader");
            else
            {
                SquadManager.SetLocked(ctx.Caller!.Squad, true);
                ctx.Reply("squad_locked");
            }
        }
        else if (ctx.MatchParameter(0, "unlock"))
        {
            if (ctx.Caller!.Squad is null || ctx.Caller!.Squad.Leader.Steam64 != ctx.CallerID)
                ctx.Reply("squad_e_notsquadleader");
            else
            {
                SquadManager.SetLocked(ctx.Caller!.Squad, false);
                ctx.Reply("squad_unlocked");
            }
        }
        else ctx.SendCorrectUsage(Syntax);
    }
}