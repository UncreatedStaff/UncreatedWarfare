using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Tweaks;

internal sealed class KeepPlayerStateOnRejoinTweak : IAsyncEventListener<PlayerJoined>
{
    private readonly Layout _layout;
    private readonly IKitDataStore _kitDataStore;
    private readonly EventDispatcher _eventDispatcher;

    public KeepPlayerStateOnRejoinTweak(Layout layout, IKitDataStore kitDataStore, EventDispatcher eventDispatcher)
    {
        _layout = layout;
        _kitDataStore = kitDataStore;
        _eventDispatcher = eventDispatcher;
    }

    [EventListener(RequireActiveLayout = true, Priority = int.MaxValue)]
    async UniTask IAsyncEventListener<PlayerJoined>.HandleEventAsync(PlayerJoined e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.SaveData.LastGameId != _layout.LayoutId)
            return;

        if (e.SaveData.TeamId != 0)
        {
            Team team = _layout.TeamManager.GetTeam(new CSteamID(e.SaveData.TeamId));
            if (team.IsValid)
            {
                await _layout.TeamManager.JoinTeamAsync(e.Player, team, token);
            }
        }

        if (e.SaveData.KitId != 0)
        {
            Kit? kit = await _kitDataStore.QueryKitAsync(e.SaveData.KitId, KitInclude.Giveable, token);

            e.Player.Component<KitPlayerComponent>().UpdateKit(kit);

            if (kit != null)
            {
                _ = _eventDispatcher.DispatchEventAsync(new PlayerKitChanged
                {
                    Player = e.Player,
                    KitId = e.SaveData.KitId,
                    Class = kit.Class,
                    Kit = kit,
                    KitName = kit.Id,
                    WasRequested = false
                }, CancellationToken.None);
            }
        }
        else
        {
            e.Player.Component<KitPlayerComponent>().UpdateKit(null);
        }
    }
}