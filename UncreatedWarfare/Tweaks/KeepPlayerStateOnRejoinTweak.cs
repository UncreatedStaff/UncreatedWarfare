#if DEBUG
//#define JOIN_DEBUG_LOGGING
#endif

using System;
using System.Diagnostics;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Tweaks;

internal sealed class KeepPlayerStateOnRejoinTweak : IAsyncEventListener<PlayerJoined>, IEventListener<PlayerJoined>
{
    private readonly Layout _layout;
    private readonly IKitDataStore _kitDataStore;
    private readonly KitRequestService _kitRequestService;
    private readonly EventDispatcher _eventDispatcher;

    public KeepPlayerStateOnRejoinTweak(Layout layout, IKitDataStore kitDataStore, EventDispatcher eventDispatcher, KitRequestService kitRequestService)
    {
        _layout = layout;
        _kitDataStore = kitDataStore;
        _eventDispatcher = eventDispatcher;
        _kitRequestService = kitRequestService;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (e.SaveData.LastGameId != _layout.LayoutId)
        {
            WriteDebugLog("Giving default kit");
            _kitRequestService.TryGiveDefaultKitMainThread(e.Player);
        }
    }

    [EventListener(RequireActiveLayout = true, Priority = int.MaxValue)]
    async UniTask IAsyncEventListener<PlayerJoined>.HandleEventAsync(PlayerJoined e, IServiceProvider serviceProvider, CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);

        if (e.SaveData.LastGameId != _layout.LayoutId)
        {
            WriteDebugLog("NOT SAME GAME");
            await _layout.TeamManager.JoinTeamAsync(e.Player, Team.NoTeam, wasByAdminCommand: false, token);
            return;
        }

        WriteDebugLog("SAME GAME");
        bool changedTeam = false;
        if (e.SaveData.TeamId != 0)
        {
            WriteDebugLog("joining old team");
            Team team = _layout.TeamManager.GetTeam(new CSteamID(e.SaveData.TeamId));
            if (team.IsValid)
            {
                changedTeam = true;
                await _layout.TeamManager.JoinTeamAsync(e.Player, team, wasByAdminCommand: false, token);
                await UniTask.SwitchToMainThread(token);
            }
        }

        if (!changedTeam && e.Player.UnturnedPlayer.quests.isMemberOfAGroup)
        {
            WriteDebugLog("leaving group");
            e.Player.UnturnedPlayer.quests.leaveGroup(true);
        }

        CurrentKitState? kitState = e.SaveData.KitState;
        if (kitState != null)
        {
            WriteDebugLog($"reinstating kit: {kitState.Id}");
            Kit? kit = await _kitDataStore.QueryKitAsync(kitState.Key, KitInclude.Giveable, token);
            if (kitState.PreviewFallback != null)
            {
                WriteDebugLog($"reinstating fallback kit: {kitState.PreviewFallback.Id}");
                Kit? fallbackKit = await _kitDataStore.QueryKitAsync(kitState.PreviewFallback.Key, KitInclude.Giveable, token);
                if (fallbackKit == null)
                    kitState.PreviewFallback = null;
                else
                    kitState.PreviewFallback.CachedKit = fallbackKit;
            }

            KitPlayerComponent component = e.Player.Component<KitPlayerComponent>();
            if (kit == null)
            {
                kitState = null;
                e.SaveData.KitState = null;
                component.UpdateKit(null);
            }
            else
            {
                kitState.UpdateCachedKit(kit);
                component.UpdateKit(kitState);
            }

            if (kit != null)
            {
                _ = _eventDispatcher.DispatchEventAsync(new PlayerKitChanged
                {
                    Player = e.Player,
                    State = kitState,
                    WasRequested = false
                }, CancellationToken.None);
            }
        }
        else
        {
            WriteDebugLog("clearing kit");
            e.Player.Component<KitPlayerComponent>().UpdateKit(null);
        }
    }

    [Conditional("JOIN_DEBUG_LOGGING")]
    private static void WriteDebugLog(string message)
    {
        WarfareModule.Singleton.ServiceProvider.Resolve<ILogger<KeepPlayerStateOnRejoinTweak>>()
            .LogDebug(message);
    }
}