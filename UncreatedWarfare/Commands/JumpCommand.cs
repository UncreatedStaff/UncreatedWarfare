using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("jump", "jmp")]
[MetadataFile(nameof(GetHelpMetadata))]
public class JumpCommand : IExecutableCommand
{
    private const string Syntax = "/jump [player] <multiplier>";
    private const string Help = "Sets a player's jump modifier.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("player", typeof(IPlayer))
                {
                    Description = "The player whose jump multiplier changes. Omit to target yourself.",
                    Parameters =
                    [
                        new CommandParameter("mulitplier", typeof(float))
                        {
                            Description = "Change the jump multiplier of the target player."
                        },
                        new CommandParameter("default")
                        {
                            Aliases = [ "reset" ],
                            Description = "Set the jump multiplier to 1x."
                        }
                    ]
                },
                new CommandParameter("mulitplier", typeof(float))
                {
                    Description = "Change the jump multiplier of the target player."
                },
                new CommandParameter("default")
                {
                    Aliases = [ "reset" ],
                    Description = "Set the jump multiplier to 1x."
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1, Syntax);
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertOnDuty();

        WarfarePlayer? target = Context.Player;
        
        if (Context.HasArgs(2) && (!Context.TryGet(0, out _, out target) || target == null))
        {
            throw Context.Reply(T.PlayerNotFound);
        }

        if (target == null)
        {
            throw Context.SendPlayerOnlyError();
        }

        int multParamIndex = Context.HasArgs(2) ? 1 : 0;
        if (!Context.TryGet(multParamIndex, out float multiplier))
        {
            if (!Context.MatchParameter(multParamIndex, "reset", "default"))
                throw Context.Reply(T.JumpMultiplierInvalidValue, Context.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.UnturnedPlayer.movement.pluginJumpMultiplier == multiplier)
        {
            throw Context.Reply(T.JumpMultiplierAlreadySet, multiplier);
        }

        target.UnturnedPlayer.movement.sendPluginJumpMultiplier(multiplier);
        Context.Reply(T.SetJumpMultiplier, multiplier, target);
        return default;
    }
} 
