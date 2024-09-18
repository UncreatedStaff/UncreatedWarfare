using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Tweaks;

namespace Uncreated.Warfare.Commands;

[Command("god")]
[MetadataFile(nameof(GetHelpMetadata))]
public class GodCommand : IExecutableCommand
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
            Description = "Toggles your ability to take damage."
        };
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

            Context.Reply(T.GodModeEnabled);
        }
        else
        {
            Context.Reply(T.GodModeDisabled);
        }
    }
}