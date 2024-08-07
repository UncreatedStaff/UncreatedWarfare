using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("speed")]
[MetadataFile(nameof(GetHelpMetadata))]
public class SpeedCommand : IExecutableCommand
{
    private const string Syntax = "/speed [player] <multiplier>";
    private const string Help = "Sets a player's speed modifier.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Set admin movement speed.",
            Parameters =
            [
                new CommandParameter("player", typeof(IPlayer))
                {
                    Description = "The player whose speed multiplier changes. Omit to target yourself.",
                    Parameters =
                    [
                        new CommandParameter("mulitplier", typeof(float))
                        {
                            Description = "Change the speed multiplier of the target player."
                        },
                        new CommandParameter("default")
                        {
                            Aliases = [ "reset" ],
                            Description = "Set the speed multiplier to 1x."
                        }
                    ]
                },
                new CommandParameter("mulitplier", typeof(float))
                {
                    Description = "Change the speed multiplier of the target player."
                },
                new CommandParameter("default")
                {
                    Aliases = [ "reset" ],
                    Description = "Set the speed multiplier to 1x."
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

        UCPlayer? target = Context.Player;

        if (Context.HasArgs(2) && (!Context.TryGet(0, out _, out target) || target == null))
        {
            throw Context.Reply(T.PlayerNotFound);
        }

        if (target == null) // ran by console
        {
            throw Context.SendPlayerOnlyError();
        }

        int multParamIndex = Context.HasArgs(2) ? 1 : 0;
        if (!Context.TryGet(multParamIndex, out float multiplier))
        {
            if (!Context.MatchParameter(multParamIndex, "reset", "default"))
                throw Context.Reply(T.SpeedMultiplierInvalidValue, Context.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.Player.movement.pluginSpeedMultiplier == multiplier)
        {
            throw Context.Reply(T.SpeedMultiplierAlreadySet, multiplier);
        }

        target.Player.movement.sendPluginSpeedMultiplier(multiplier);
        Context.Reply(T.SetSpeedMultiplier, multiplier, target);
        return default;
    }
}