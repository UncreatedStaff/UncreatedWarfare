using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Commands;

[Command("quickcap"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugQuickCapCommand : IExecutableCommand
{
    private readonly IFlagRotationService _flagRotationService;
    public required CommandContext Context { get; init; }

    public DebugQuickCapCommand(IFlagRotationService flagRotationService)
    {
        _flagRotationService = flagRotationService;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.Player.Team.IsValid)
        {
            throw Context.ReplyString("Not on a team.");
        }

        IEnumerable<FlagObjective> obj = _flagRotationService.EnumerateObjectives();
        bool cascade = false;
        if (_flagRotationService is DualSidedFlagService d)
        {
            cascade = true;
            if (!d.EndingTeam.IsFriendly(Context.Player.Team))
                obj = obj.Reverse();
        }

        bool shouldCapture = false;
        List<FlagObjective> objsToCapture = new List<FlagObjective>();
        foreach (FlagObjective flag in obj)
        {
            if (!shouldCapture && !flag.Region.TestPoint(Context.Player.Position))
                continue;

            objsToCapture.Add(flag);
            if (!cascade)
                break;

            shouldCapture = true;
        }

        for (int i = objsToCapture.Count - 1; i >= 0; --i)
        {
            FlagObjective flag = objsToCapture[i];
            flag.Contest.AwardPoints(Context.Player.Team, flag.Contest.MaxPossiblePoints);
            Context.ReplyString($"Captured {flag.Region.Name}.");
        }

        if (!Context.Responded)
        {
            Context.ReplyString("Not standing in a zone.");
        }
    }
}