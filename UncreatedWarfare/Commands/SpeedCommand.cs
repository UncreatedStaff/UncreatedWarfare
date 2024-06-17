using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class SpeedCommand : Command
{
    private const string Syntax = "/speed [player] <multiplier>";
    private const string Help = "Sets a player's speed modifier.";

    public SpeedCommand() : base("speed", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = "Set admin movement speed.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("player", typeof(IPlayer))
                {
                    Description = "The player whose speed multiplier changes. Omit to target yourself.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("mulitplier", typeof(float))
                        {
                            Description = "Change the speed multiplier of the target player."
                        },
                        new CommandParameter("default")
                        {
                            Aliases = new string[] { "reset" },
                            Description = "Set the speed multiplier to 1x."
                        }
                    }
                },
                new CommandParameter("mulitplier", typeof(float))
                {
                    Description = "Change the speed multiplier of the target player."
                },
                new CommandParameter("default")
                {
                    Aliases = new string[] { "reset" },
                    Description = "Set the speed multiplier to 1x."
                }
            }
        };
    }

    public override void Execute(CommandContext ctx)
    {
        ctx.AssertArgs(1, Syntax);
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        ctx.AssertOnDuty();

        UCPlayer? target = ctx.Caller;

        if (ctx.HasArgs(2) && (!ctx.TryGet(0, out _, out target) || target == null))
            throw ctx.Reply(T.PlayerNotFound);

        if (target == null) // ran by console
            throw ctx.SendPlayerOnlyError();

        int multParamIndex = ctx.HasArgs(2) ? 1 : 0;
        if (!ctx.TryGet(multParamIndex, out float multiplier))
        {
            if (!ctx.MatchParameter(multParamIndex, "reset", "default"))
                throw ctx.Reply(T.SpeedMultiplierInvalidValue, ctx.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.Player.movement.pluginSpeedMultiplier == multiplier)
            throw ctx.Reply(T.SpeedMultiplierAlreadySet, multiplier);

        target.Player.movement.sendPluginSpeedMultiplier(multiplier);
        ctx.Reply(T.SetSpeedMultiplier, multiplier, target);
    }
}