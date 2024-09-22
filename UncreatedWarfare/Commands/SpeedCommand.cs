using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("speed")]
public class SpeedCommand : IExecutableCommand
{
    private readonly SpeedCommandTranslations _translations;

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

    public SpeedCommand(TranslationInjection<SpeedCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1);

        WarfarePlayer? target = Context.Player;

        if (Context.HasArgs(2) && (!Context.TryGet(0, out _, out target) || target == null))
        {
            throw Context.SendPlayerNotFound();
        }

        if (target == null) // ran by console
        {
            throw Context.SendPlayerOnlyError();
        }

        int multParamIndex = Context.HasArgs(2) ? 1 : 0;
        if (!Context.TryGet(multParamIndex, out float multiplier))
        {
            if (!Context.MatchParameter(multParamIndex, "reset", "default"))
                throw Context.Reply(_translations.SpeedMultiplierInvalidValue, Context.Get(multParamIndex)!);

            multiplier = 1f;
        }

        multiplier = Mathf.Clamp(multiplier, 0f, 10f);

        if (target.UnturnedPlayer.movement.pluginSpeedMultiplier == multiplier)
        {
            throw Context.Reply(_translations.SpeedMultiplierAlreadySet, multiplier);
        }

        target.UnturnedPlayer.movement.sendPluginSpeedMultiplier(multiplier);
        Context.Reply(_translations.SetSpeedMultiplier, multiplier, target);
        return default;
    }
}

public class SpeedCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Speed";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<string> SpeedMultiplierInvalidValue = new Translation<string>("<#b3a6a2>Speed multiplier <#fff>{0}</color> is invalid.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<float> SpeedMultiplierAlreadySet = new Translation<float>("<#b3a6a2>Speed multiplier is already set to <#fff>{0}</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation<float, IPlayer> SetSpeedMultiplier = new Translation<float, IPlayer>("<#d1bda7>Set {0}'s speed multiplier to <#fff>{0}</color>.", arg0Fmt: "0.##", arg1Fmt: WarfarePlayer.FormatColoredCharacterName);
}