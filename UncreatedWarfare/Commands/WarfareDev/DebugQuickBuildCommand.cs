using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("quickbuild", "qb"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugQuickBuildCommand : IExecutableCommand
{
    private readonly FobManager _fobManager;
    public required CommandContext Context { get; init; }

    public DebugQuickBuildCommand(FobManager fobManager)
    {
        _fobManager = fobManager;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBuildableTarget(out IBuildable? buildable))
        {
            throw Context.ReplyString("Look at a shovelable buildable.");
        }

        ShovelableBuildable? shovelable = _fobManager.Entities.OfType<ShovelableBuildable>().FirstOrDefault(x => x.Buildable.Equals(buildable));
        if (shovelable == null)
        {
            throw Context.ReplyString("Look at a shovelable buildable.");
        }

        shovelable.Shovel(Context.Player, buildable.Position, shovelable.HitsRemaining);

        throw Context.ReplyString($"Built <#ddd>{shovelable.Info.CompletedStructure}</color>.");
    }
}