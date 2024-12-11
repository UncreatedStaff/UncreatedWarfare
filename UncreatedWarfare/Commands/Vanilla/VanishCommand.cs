using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[Command("vanish"), MetadataFile]
internal sealed class VanishCommand : IExecutableCommand
{
    private readonly VanishCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VanishCommand(TranslationInjection<VanishCommandTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        await Context.AssertPermissions(VanishPlayerComponent.VanishPermission, token);

        await UniTask.SwitchToMainThread(token);
        
        VanishPlayerComponent component = Context.Player.Component<VanishPlayerComponent>();

        component.IsActive = !component.IsActive;

        if (component.IsActive)
        {
            Context.Reply(_translations.VanishModeEnabled);
        }
        else
        {
            Context.Reply(_translations.VanishModeDisabled);
        }
    }
}

public class VanishCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Vanish";

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation VanishModeEnabled = new Translation("<#bfb9ac>Vanish mode <#99ff66>enabled</color>.");

    [TranslationData(IsPriorityTranslation = false)]
    public readonly Translation VanishModeDisabled = new Translation("<#ff9966>Vanish mode <#ff9999>disabled</color>.");
}