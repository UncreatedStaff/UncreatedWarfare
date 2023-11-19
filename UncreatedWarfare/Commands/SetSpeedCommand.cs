using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class SetSpeedCommand : Command {
    private const string SYNTAX = "/setspeed <PlayerName> <SpeedMultiplier>";
    private const string HELP = "Sets a player's speed modifier.";

    public SetSpeedCommand() : base("setspeed", EAdminType.MEMBER) {
        Structure = new CommandStructure {
            Description = HELP,
            Parameters = new CommandParameter[] {
                new CommandParameter("player", typeof(IPlayer)) {
                    Parameters = new CommandParameter[] {
                        new CommandParameter("mulitplier", typeof(byte))
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx) {
        ctx.AssertArgs(2, SYNTAX);

        if (!ctx.TryGet(0, out ulong Id, out UCPlayer? target)) {
            throw ctx.Reply(T.PlayerNotFound);
        }

        if (int.TryParse(ctx.Get(2), out int multiplier)) {
            throw ctx.Reply(T.OptionsInvalidValue);
        }

        if (target != null && target.IsOnline) {
            throw ctx.Reply(T.PlayerNotFound);
        }

        if (multiplier > 10) {
            throw ctx.Reply(T.InvalidModifier);
        }

        target.Player.movement.pluginSpeedMultiplier = multiplier;
        return;
    }
}
