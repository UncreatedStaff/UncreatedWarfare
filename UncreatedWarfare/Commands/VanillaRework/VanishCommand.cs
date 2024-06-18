using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;

namespace Uncreated.Warfare.Commands;

[Command("vanish")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class VanishCommand : IExecutableCommand
{
    private const string Syntax = "/vanish";
    private const string Help = "Toggle your visibility to other players.";

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
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertRanByPlayer();

        Context.AssertOnDuty();

        bool isUnvanished = Context.Player.Player.movement.canAddSimulationResultsToUpdates;
        Context.Player.VanishMode = isUnvanished;
        if (isUnvanished)
        {
            Context.Reply(T.VanishModeEnabled);
        }
        else
        {
            Context.Reply(T.VanishModeDisabled);
        }
    }
}