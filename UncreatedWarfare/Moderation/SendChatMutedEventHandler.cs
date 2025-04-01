using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;

namespace Uncreated.Warfare.Moderation;

/// <summary>
/// Handles stopping chat messages from muted players.
/// </summary>
internal class SendChatMutedEventHandler : IAsyncEventListener<PlayerChatRequested>
{
    [EventListener(Priority = 1)]
    async UniTask IAsyncEventListener<PlayerChatRequested>.HandleEventAsync(PlayerChatRequested e, IServiceProvider serviceProvider, CancellationToken token)
    {
        if (e.HasAdminChatPermissions)
        {
            return;
        }

        PlayerModerationCacheComponent moderation = e.Player.Component<PlayerModerationCacheComponent>();
        if (moderation.TextMuteExpiryTime <= DateTime.UtcNow)
        {
            return;
        }

        // double check still muted
        await moderation.RefreshActiveMute().ConfigureAwait(false);
        if (moderation.TextMuteExpiryTime <= DateTime.UtcNow)
        {
            return;
        }

        // muted todo feedback in chat
        e.Cancel();
    }
}
