using System;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Requests;

public class PunchToRequestTweaks : IAsyncEventListener<PlayerPunched>
{
    private const float UnturnedPunchDistance = 1.75f;
    private readonly SignInstancer _signInstancer;
    private readonly ILogger _logger;

    public PunchToRequestTweaks(SignInstancer signInstancer, ILogger<PunchToRequestTweaks> logger)
    {
        _signInstancer = signInstancer;
        _logger = logger;
    }
    
    public async UniTask HandleEventAsync(PlayerPunched e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
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

    private bool TryGetTargetRootTransform(WarfarePlayer player, [MaybeNullWhen(false)] out Transform transform)
    {
        Transform aim = player.UnturnedPlayer.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), UnturnedPunchDistance, RayMasks.DAMAGE_CLIENT & ~RayMasks.ENEMY, player.UnturnedPlayer);
        transform = info.transform.root;
        return transform != null;
    }
}