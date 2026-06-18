using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Players.UI;

public class DutyUI : UnturnedUI, IHudUIListener
{
    private readonly IPlayerService _playerService;
    private readonly HudManager _hudManager;
    private readonly ModerationTranslations _translations;
    private bool _subbedUpdate;

    private bool _wasVoting;

    private static StaticGetter<bool>? _getIsVoting;

    private readonly UnturnedUIElement _positionNoVote = new UnturnedUIElement("LogicPositionNoVote");

    private readonly UnturnedUIElement _positionVote = new UnturnedUIElement("LogicPositionVote");

    public DutyUI(
        AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        IPlayerService playerService,
        TranslationInjection<ModerationTranslations> translations,
        HudManager hudManager
    ) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:DutyUI"), staticKey: true)
    {
        _playerService = playerService;
        _hudManager = hudManager;
        _translations = translations.Value;
        _hudManager.OnPluginVotingUpdated += PluginVotingUpdated;
        if (ChatManager.voteAllowed)
        {
            _getIsVoting ??= Accessor.GenerateStaticGetter<ChatManager, bool>("isVoting", throwOnError: false);
        }

        TimeUtility.updated += UpdateVotePosition;
        _subbedUpdate = true;
    }

    protected override void OnDisposing()
    {
        _hudManager.OnPluginVotingUpdated -= PluginVotingUpdated;
        if (_subbedUpdate)
        {
            TimeUtility.updated -= UpdateVotePosition;
            _subbedUpdate = false;
        }
    }

    private void PluginVotingUpdated(WarfarePlayer player, bool isPluginVoting)
    {
        if (!player.IsOnDuty)
            return;

        UpdateVotePosition();
        UnturnedUIElement voteLogicElement = isPluginVoting || _wasVoting ? _positionVote : _positionNoVote;
        voteLogicElement.Show(player);
    }

    private void UpdateVotePosition()
    {
        bool isVoting = _getIsVoting != null && _getIsVoting();
        if (_wasVoting == isVoting)
            return;

        _wasVoting = isVoting;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (!player.IsOnDuty)
                continue;

            bool isPluginVoting = _hudManager.GetIsPluginVoting(player);

            UnturnedUIElement voteLogicElement = isPluginVoting || isVoting ? _positionVote : _positionNoVote;
            voteLogicElement.Show(player);
        }
    }

    public void SendToPlayer(WarfarePlayer player)
    {
        SendToPlayer(player.Connection, _translations.OnDutyUI.Translate(player));
        UpdateVotePosition();
        if (_wasVoting || _hudManager.GetIsPluginVoting(player))
        {
            _positionVote.Show(player);
        }
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
            }
            
            return;
        }

        foreach (WarfarePlayer pl in _playerService.OnlinePlayers)
        {
            if (!pl.IsOnDuty)
                continue;

            SendToPlayer(pl);
        }
    }
}