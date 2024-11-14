using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("effectattach", "ea"), SubCommandOf(typeof(WarfareDevCommand))]
internal class DebugEffectAttach : IExecutableCommand
{
    private readonly WorldIconManager _iconManager;
    public CommandContext Context { get; set; }

    public DebugEffectAttach(WorldIconManager iconManager)
    {
        _iconManager = iconManager;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetTargetInfo(out RaycastInfo? raycast, distance: 8192f))
            throw Context.ReplyString("Not looking at anything.");

        IAssetLink<EffectAsset> effect = AssetLink.Create<EffectAsset>(new Guid("db94515c46c144019510790f4857e50d"));

        WarfarePlayer? targetPlayer = Context.MatchFlag("-p") ? Context.Player : null;
        Team? targetTeam = Context.Player.Team.IsValid ? Context.Player.Team : null;
        Context.TryGet(0, out float lifetime);
        if (!Context.TryGet(1, out float tickSpeed))
            tickSpeed = WorldIconManager.DefaultTickSpeed;

        WorldIconInfo info;
        if (raycast.transform.gameObject.layer == (int)ELayerMask.GROUND)
        {
            info = new WorldIconInfo(raycast.point, effect, targetTeam, targetPlayer, null, lifetime);
        }
        else
        {
            info = new WorldIconInfo(raycast.transform, effect, targetTeam, targetPlayer, null, lifetime);
        }

        info.TickSpeed = tickSpeed;

        _iconManager.CreateIcon(info);
        return UniTask.CompletedTask;
    }
}