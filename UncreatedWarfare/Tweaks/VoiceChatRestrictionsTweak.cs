using System;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Tweaks;

internal sealed class VoiceChatRestrictionsTweak : IHostedService
{
    private readonly IPlayerService _playerService;

    public VoiceChatRestrictionsTweak(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice += PlayerVoiceOnRelayVoice;
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice -= PlayerVoiceOnRelayVoice;
        return UniTask.CompletedTask;
    }

    private void PlayerVoiceOnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(speaker);
        PlayerModerationCacheComponent comp = player.Component<PlayerModerationCacheComponent>();

        if (!shouldAllow)
            return;

        bool isMuted = comp.VoiceMuteExpiryTime > DateTime.UtcNow;
        shouldAllow = !isMuted;
    }
}