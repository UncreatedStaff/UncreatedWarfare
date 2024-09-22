using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("range", "r")]
public class RangeCommand : IExecutableCommand
{
    private readonly RangeCommandTranslations _translations;
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

    public RangeCommand(TranslationInjection<RangeCommandTranslations> translations)
    {
        _translations = translations.Value;
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
            throw Context.Reply(_translations.RangeOutput, distance);
#if false
        }

        if (Context.Player.Squad is null)
            throw Context.Reply(_translations.RangeNotInSquad);

        UCPlayer squadLeader = Context.Player.Squad.Leader;

        if (!squadLeader.Player.quests.isMarkerPlaced)
            throw Context.Reply(_translations.RangeNoMarker);

        distance = Mathf.RoundToInt((Context.Player.Position - squadLeader.Player.quests.markerPosition).magnitude / Precision) * Precision;
        throw Context.Reply(_translations.RangeOutput, distance);
#endif
    }
}

public class RangeCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Range";

    [TranslationData("Displays the distance from the caller's position to their squad's waypoint.", "Distance in meters")]
    public readonly Translation<float> RangeOutput = new Translation<float>("<#9e9c99>The range to your squad's marker is: <#8aff9f>{0}m</color>.", arg0Fmt: "N0");

    [TranslationData("Output if there is no waypoint placed.")]
    public readonly Translation RangeNoMarker = new Translation("<#9e9c99>You squad has no marker.");

    [TranslationData("Output if the player is not the squad leader.")]
    public readonly Translation RangeNotSquadleader = new Translation("<#9e9c99>Only <#cedcde>SQUAD LEADERS</color> can place markers.");

    [TranslationData("Output if the player is not in a squad.")]
    public readonly Translation RangeNotInSquad = new Translation("<#9e9c99>You must JOIN A SQUAD in order to do /range.");
}