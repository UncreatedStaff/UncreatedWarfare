using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("range", "r")]
[MetadataFile(nameof(GetHelpMetadata))]
public class RangeCommand : IExecutableCommand
{
    private const int Precision = 10;
    
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = $"Shows you how far away you are from your squad leader's marker within {Precision} meters."
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        int distance;
#if false
        if (!Data.Is<ISquads>())
        {
#endif
            distance = Mathf.RoundToInt((Context.Player.Position - Context.Player.UnturnedPlayer.quests.markerPosition).magnitude / Precision) * Precision;
            throw Context.Reply(T.RangeOutput, distance);
#if false
        }

        if (Context.Player.Squad is null)
            throw Context.Reply(T.RangeNotInSquad);

        UCPlayer squadLeader = Context.Player.Squad.Leader;

        if (!squadLeader.Player.quests.isMarkerPlaced)
            throw Context.Reply(T.RangeNoMarker);

        distance = Mathf.RoundToInt((Context.Player.Position - squadLeader.Player.quests.markerPosition).magnitude / Precision) * Precision;
        throw Context.Reply(T.RangeOutput, distance);
#endif
    }
}
