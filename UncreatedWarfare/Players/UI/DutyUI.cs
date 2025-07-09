using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.UI;

public class DutyUI : UnturnedUI, IHudUIListener
{
    private readonly IPlayerService _playerService;
    private readonly ModerationTranslations _translations;
    private bool _subbedUpdate;

    private bool _votePosition;

    private static StaticGetter<bool>? _getIsVoting;

    private readonly UnturnedUIElement _positionNoVote = new UnturnedUIElement("LogicPositionNoVote");

    private readonly UnturnedUIElement _positionVote = new UnturnedUIElement("LogicPositionVote");

    public DutyUI(AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        IPlayerService playerService,
        TranslationInjection<ModerationTranslations> translations
        )
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:DutyUI"), staticKey: true)
    {
        _playerService = playerService;
        _translations = translations.Value;
        if (ChatManager.voteAllowed)
        {
            _getIsVoting ??= Accessor.GenerateStaticGetter<ChatManager, bool>("isVoting", throwOnError: false);
            TimeUtility.updated += OnUpdate;
            _subbedUpdate = true;
        }
    }

    protected override void OnDisposing()
    {
        if (_subbedUpdate)
        {
            TimeUtility.updated -= OnUpdate;
            _subbedUpdate = false;
        }
    }

    private void OnUpdate()
    {
        if (_getIsVoting == null || !_getIsVoting())
        {
            if (!_votePosition)
                return;

            _votePosition = false;
        }
        else
        {
            if (_votePosition)
                return;

            _votePosition = true;
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (player.IsOnDuty)
                (_votePosition ? _positionVote : _positionNoVote).SetVisibility(player.Connection, true);
        }
    }

    public void SendToPlayer(WarfarePlayer player)
    {
        SendToPlayer(player.Connection, _translations.OnDutyUI.Translate(player));
        if (_votePosition)
            _positionVote.SetVisibility(player, true);
    }

    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
            ClearFromPlayer(player.Connection);
        else
            ClearFromAllPlayers();
    }

    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            if (player.IsOnDuty)
            {
                SendToPlayer(player);
                if (_votePosition)
                    _positionVote.SetVisibility(player.Connection, true);
            }
            
            return;
        }

        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            if (!pl.IsOnDuty)
                continue;

            SendToPlayer(pl);
            if (_votePosition)
                _positionVote.SetVisibility(pl.Connection, true);
        }
    }
}