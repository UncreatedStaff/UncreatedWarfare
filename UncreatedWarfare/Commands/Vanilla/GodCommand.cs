using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[Command("god")]
[MetadataFile(nameof(GetHelpMetadata))]
public class GodCommand : IExecutableCommand
{
    private readonly GodCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Toggles your ability to take damage."
        };
    }

    public GodCommand(TranslationInjection<GodCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertOnDuty();

        await Context.AssertPermissions(GodPlayerComponent.GodPermission, token);

        await UniTask.SwitchToMainThread(token);

        GodPlayerComponent component = Context.Player.Component<GodPlayerComponent>();

        component.IsActive = !component.IsActive;

        if (component.IsActive)
        {
            Context.Player.UnturnedPlayer.life.sendRevive();

            PlayerInjureComponent? injureComponent = Context.Player.ComponentOrNull<PlayerInjureComponent>();
            if (injureComponent != null)
                injureComponent.Revive();

            Context.Reply(_translations.GodModeEnabled);
        }
        else
        {
            Context.Reply(_translations.GodModeDisabled);
        }
    }
}

public class GodCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/God";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation GodModeEnabled = new Translation("<#bfb9ac>God mode <#99ff66>enabled</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation GodModeDisabled = new Translation("<#ff9966>God mode <#ff9999>disabled</color>.");
}