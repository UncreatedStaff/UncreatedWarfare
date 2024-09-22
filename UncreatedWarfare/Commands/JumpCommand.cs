using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("jump", "jmp")]
public class JumpCommand : IExecutableCommand
{
    private readonly JumpCommandTranslations _translations;
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

    public JumpCommand(TranslationInjection<JumpCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1, Syntax);

        WarfarePlayer? target = Context.Player;
        
        if (Context.HasArgs(2) && (!Context.TryGet(0, out _, out target) || target == null))
        {
            throw Context.SendPlayerNotFound();
        }

        if (target == null)
        {
            throw Context.SendPlayerOnlyError();
        }

        int multParamIndex = Context.HasArgs(2) ? 1 : 0;
        if (!Context.TryGet(multParamIndex, out float multiplier))
        {
            if (!Context.MatchParameter(multParamIndex, "reset", "default"))
                throw Context.Reply(_translations.JumpMultiplierInvalidValue, Context.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.UnturnedPlayer.movement.pluginJumpMultiplier == multiplier)
        {
            throw Context.Reply(_translations.JumpMultiplierAlreadySet, multiplier);
        }

        target.UnturnedPlayer.movement.sendPluginJumpMultiplier(multiplier);
        Context.Reply(_translations.SetJumpMultiplier, multiplier, target);
        return default;
    }
}

public class JumpCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Jump";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> JumpMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Jump multiplier <#fff>{0}</color> is invalid.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<float> JumpMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Jump multiplier is already set to <#fff>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<float, IPlayer> SetJumpMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", arg0Fmt: "0.##", arg1Fmt: WarfarePlayer.FormatColoredCharacterName);
}