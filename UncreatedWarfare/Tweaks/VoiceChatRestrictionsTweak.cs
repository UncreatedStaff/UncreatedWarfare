using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Tweaks;

internal sealed class VoiceChatRestrictionsTweak : IHostedService
{
    private readonly IPlayerService _playerService;
    private readonly ModerationTranslations _translations;
    private readonly IAssetLink<EffectAsset> _mutedUi;

    public VoiceChatRestrictionsTweak(IPlayerService playerService, AssetConfiguration assetConfiguration, TranslationInjection<ModerationTranslations> translations)
    {
        _playerService = playerService;
        _translations = translations.Value;

        _mutedUi = assetConfiguration.GetAssetLink<EffectAsset>("UI:MutedUI");
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice += PlayerVoiceOnRelayVoice;
        TimeUtility.updated += Update;
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask StopAsync(CancellationToken token)
    {
        PlayerVoice.onRelayVoice -= PlayerVoiceOnRelayVoice;
        TimeUtility.updated -= Update;
        return UniTask.CompletedTask;
    }

    private void Update()
    {
        float rt = 0;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            PlayerModerationCacheComponent component = player.Component<PlayerModerationCacheComponent>();

            if (component.LastMuteUI < 0)
                continue;

            if (rt == 0)
                rt = Time.realtimeSinceStartup;

            if (rt - component.LastMuteUI <= 1)
                continue;

            // clear UI after one second of not talking
            if (_mutedUi.TryGetId(out ushort id))
            {
                EffectManager.askEffectClearByID(id, player.Connection);
            }

            component.LastMuteUI = -1f;
        }
    }

    private void PlayerVoiceOnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        // todo: if nelson changes the voice thing use PlayerVoice.customAllowTalking
        WarfarePlayer player = _playerService.GetOnlinePlayer(speaker);
        PlayerModerationCacheComponent comp = player.Component<PlayerModerationCacheComponent>();

        bool isMuted;
        if (!shouldAllow)
        {
            isMuted = true;
        }
        else
        {
            isMuted = comp.VoiceMuteExpiryTime > DateTime.UtcNow;
        }

        shouldAllow = isMuted;

        if (isMuted == comp.LastMuteUI >= 0)
            return;

        if (isMuted)
        {
            if (_mutedUi.TryGetId(out ushort id))
            {
                EffectManager.sendUIEffect(id, -1, speaker.channel.owner.transportConnection, reliable: true, _translations.MutedUI.Translate(player));
            }

            comp.LastMuteUI = Time.realtimeSinceStartup;
        }
        else
        {
            if (_mutedUi.TryGetId(out ushort id))
            {
                EffectManager.askEffectClearByID(id, speaker.channel.owner.transportConnection);
            }

            comp.LastMuteUI = -1f;
        }
    }
}
