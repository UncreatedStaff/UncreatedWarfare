using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[Command("god", PermissionOverride = GodPlayerComponent.GodPermissionName), MetadataFile]
internal sealed class GodCommand : IExecutableCommand
{
    private readonly GodCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public GodCommand(TranslationInjection<GodCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

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

        return UniTask.CompletedTask;
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