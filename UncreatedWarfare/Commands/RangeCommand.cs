using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class RangeCommand : Command
{
    private const string SYNTAX = "/range";
    private const string HELP = "Shows you your distance from your squad leader's marker.";

    public RangeCommand() : base("range", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!Data.Is<ISquads>())
        {
            int distance = Mathf.RoundToInt((ctx.Caller.Position - ctx.Caller.Player.quests.markerPosition).magnitude / 10) * 10;
            throw ctx.Reply(T.RangeOutput, distance);
        }
        if (ctx.Caller.Squad is not null)
        {
            if (ctx.Caller.Squad.Leader.Player.quests.isMarkerPlaced)
            {
                int distance = Mathf.RoundToInt((ctx.Caller.Position - ctx.Caller.Squad.Leader.Player.quests.markerPosition).magnitude / 10) * 10;
                ctx.Reply(T.RangeOutput, distance);
            }
            else throw ctx.Reply(T.RangeNoMarker);
        }
        else throw ctx.Reply(T.RangeNotInSquad);
    }
}
