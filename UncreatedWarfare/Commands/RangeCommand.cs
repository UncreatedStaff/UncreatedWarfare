using Cysharp.Threading.Tasks;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

[Command("range", "r")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class RangeCommand : IExecutableCommand
{
    private const int Precision = 10;

    private const string Syntax = "/range";
    private static readonly string Help = $"Shows you how far away you are from your squad leader's marker within {Precision} meters.";

    
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        int distance;
        if (!Data.Is<ISquads>())
        {
            distance = Mathf.RoundToInt((Context.Player.Position - Context.Player.Player.quests.markerPosition).magnitude / Precision) * Precision;
            throw Context.Reply(T.RangeOutput, distance);
        }

        if (Context.Player.Squad is null)
            throw Context.Reply(T.RangeNotInSquad);

        UCPlayer squadLeader = Context.Player.Squad.Leader;

        if (!squadLeader.Player.quests.isMarkerPlaced)
            throw Context.Reply(T.RangeNoMarker);

        distance = Mathf.RoundToInt((Context.Player.Position - squadLeader.Player.quests.markerPosition).magnitude / Precision) * Precision;
        throw Context.Reply(T.RangeOutput, distance);
    }
}
