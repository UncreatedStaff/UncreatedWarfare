using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Kits.Tweaks;

[Priority(-1)]
internal class RemoveKitOnGameEnd : ILayoutHostedService
{
    private readonly KitRequestService _kitRequestService;
    private readonly IKitDataStore _kitDataStore;
    private readonly IPlayerService _playerService;

    public RemoveKitOnGameEnd(KitRequestService kitRequestService, IKitDataStore kitDataStore, IPlayerService playerService)
    {
        _kitRequestService = kitRequestService;
        _kitDataStore = kitDataStore;
        _playerService = playerService;
    }

    private async UniTask GrantDefaultKits(CancellationToken token)
    {
        Kit? defaultKit = await _kitDataStore.QueryKitAsync(KitRequestService.DefaultKitId, KitInclude.Giveable, token);
        if (defaultKit == null)
            return;

        await UniTask.SwitchToMainThread(token);

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (player.Component<KitPlayerComponent>().ActiveKitId != KitRequestService.DefaultKitId)
                _kitRequestService.GiveKitMainThread(player, new KitBestowData(defaultKit));
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return GrantDefaultKits(token);
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}