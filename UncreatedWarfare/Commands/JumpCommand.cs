using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class JumpCommand : Command
{
    private const string Syntax = "/jump [player] <multiplier>";
    private const string Help = "Sets a player's jump modifier.";

    public JumpCommand() : base("jump", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = Help,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("player", typeof(IPlayer))
                {
                    Description = "The player whose jump multiplier changes. Omit to target yourself.",
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("mulitplier", typeof(float))
                        {
                            Description = "Change the jump multiplier of the target player."
                        },
                        new CommandParameter("default")
                        {
                            Aliases = new string[] { "reset" },
                            Description = "Set the jump multiplier to 1x."
                        }
                    }
                },
                new CommandParameter("mulitplier", typeof(float))
                {
                    Description = "Change the jump multiplier of the target player."
                },
                new CommandParameter("default")
                {
                    Aliases = new string[] { "reset" },
                    Description = "Set the jump multiplier to 1x."
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertArgs(1, Syntax);
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        ctx.AssertOnDuty();

        UCPlayer? target = ctx.Caller;
        
        if (ctx.HasArgs(2) && (!ctx.TryGet(0, out ulong Id, out target) || target == null))
            throw ctx.Reply(T.PlayerNotFound);

        if (target == null)
            throw ctx.SendPlayerOnlyError();

        int multParamIndex = ctx.HasArgs(2) ? 1 : 0;
        if (!ctx.TryGet(multParamIndex, out float multiplier))
        {
            if (!ctx.MatchParameter(multParamIndex, "reset", "default"))
                throw ctx.Reply(T.JumpMultiplierInvalidValue, ctx.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.Player.movement.pluginJumpMultiplier == multiplier)
            throw ctx.Reply(T.JumpMultiplierAlreadySet, multiplier);

        target.Player.movement.sendPluginJumpMultiplier(multiplier);
        ctx.Reply(T.SetJumpMultiplier, multiplier, target);
    }
} 
