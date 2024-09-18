using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[Command("vanish")]
[MetadataFile(nameof(GetHelpMetadata))]
public class VanishCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Toggle your visibility to other players."
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertOnDuty();

        await Context.AssertPermissions(VanishPlayerComponent.VanishPermission, token);

        await UniTask.SwitchToMainThread(token);
        
        VanishPlayerComponent component = Context.Player.Component<VanishPlayerComponent>();

        component.IsActive = !component.IsActive;

        if (component.IsActive)
        {
            Context.Reply(T.VanishModeEnabled);
        }
        else
        {
            Context.Reply(T.VanishModeDisabled);
        }
    }
}