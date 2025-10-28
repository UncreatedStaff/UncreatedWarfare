using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Interaction.Requests;

public class PunchToRequestTweaks : IAsyncEventListener<PlayerPunched>
{
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
        if (e.InputInfo == null)
            return;

        // ignore punches if outside main
        IRequestable<object>? requestable = null;

        if (e.InputInfo.type is ERaycastInfoType.BARRICADE or ERaycastInfoType.STRUCTURE or ERaycastInfoType.VEHICLE or ERaycastInfoType.OBJECT)
            requestable = RequestHelper.GetRequestable(e.InputInfo.transform, _signInstancer);

        if (requestable == null)
            return;

        Vector3 pos = e.InputInfo.transform.position;
        if (!_zoneStore.IsInMainBase(pos) && !_zoneStore.IsInWarRoom(pos))
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
}