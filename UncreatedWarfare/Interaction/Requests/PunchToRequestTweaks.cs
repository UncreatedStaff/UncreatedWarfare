using System;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Interaction.Requests;

public class PunchToRequestTweaks : IAsyncEventListener<PlayerPunched>
{
    private const float UnturnedPunchDistance = 1.75f;
    private readonly SignInstancer _signInstancer;
    private readonly ILogger _logger;
    private readonly ZoneStore _zoneStore;

    public PunchToRequestTweaks(SignInstancer signInstancer, ILogger<PunchToRequestTweaks> logger, ZoneStore zoneStore)
    {
        _signInstancer = signInstancer;
        _logger = logger;
        _zoneStore = zoneStore;
    }
    
    public async UniTask HandleEventAsync(PlayerPunched e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        // ignore punches if outside main
        if (!_zoneStore.IsInMainBase(e.Player, e.Player.Team.Faction))
            return;

        IRequestable<object>? requestable = null;
        if (TryGetTargetRootTransform(e.Player, out Transform? transform))
            requestable = RequestHelper.GetRequestable(transform, _signInstancer);

        if (requestable == null)
            return;

        await RequestHelper.RequestAsync(
            e.Player,
            requestable,
            _logger,
            serviceProvider,
            typeof(RequestCommandResultHandler),
            token
        );
    }

    private static bool TryGetTargetRootTransform(WarfarePlayer player, [MaybeNullWhen(false)] out Transform transform)
    {
        Transform aim = player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), UnturnedPunchDistance, RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY, player.UnturnedPlayer);
        if (info.transform == null)
        {
            transform = null;
            return false;
        }

        transform = info.transform.root;
        return true;
    }
}