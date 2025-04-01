using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Components;

[PlayerComponent]
internal class PlayerEnemyTerritoryWarningComponent : IPlayerComponent,
    IEventListener<PlayerEnteredZone>,
    IEventListener<PlayerExitedZone>,
    IEventListener<PlayerTeamChanged>
{
    public const float AntiMainCampWarningTime = 10f;

#nullable disable

    private ZoneStore _zoneStore;
    private PlayersTranslations _translations;
    private TimeTranslations _timeTranslations;

#nullable restore

    private bool _isMainCamping;
    private Coroutine? _mainCampCoroutine;

    public required WarfarePlayer Player { get; set; }

    public bool IsMainCamping => _isMainCamping && !Player.UnturnedPlayer.life.isDead;

    public void Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<PlayersTranslations>>().Value;
        _timeTranslations = serviceProvider.GetRequiredService<TranslationInjection<TimeTranslations>>().Value;

        if (isOnJoin)
        {
            CheckIsMainCamping();
        }
    }

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        CheckIsMainCamping();
    }

    void IEventListener<PlayerEnteredZone>.HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type != ZoneType.AntiMainCampArea
            || string.Equals(e.Zone.Faction, Player.Team.Faction.FactionId, StringComparison.Ordinal))
        {
            return;
        }

        if (!_isMainCamping)
            SendMainCampWarning();
    }

    void IEventListener<PlayerExitedZone>.HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type != ZoneType.AntiMainCampArea
            || string.Equals(e.Player.Team.Faction.FactionId, e.Zone.Faction, StringComparison.Ordinal))
            return;

        CheckIsMainCamping();
    }

    private bool CalculateIsMainCamping()
    {
        if (Player.IsOnDuty)
            return false;

        foreach (Zone zone in _zoneStore.EnumerateInsideZones(Player.Position, ZoneType.AntiMainCampArea))
        {
            if (string.Equals(Player.Team.Faction.FactionId, zone.Faction, StringComparison.Ordinal))
                continue;

            return true;
        }

        return false;
    }

    private void SendMainCampWarning()
    {
        _isMainCamping = true;

        ToastMessage toast = new ToastMessage(
            ToastMessageStyle.FlashingWarning,
            _translations.EnteredEnemyTerritory.Translate(
                TimeAddon.ToLongTimeString(_timeTranslations, Mathf.RoundToInt(AntiMainCampWarningTime), Player.Locale.LanguageInfo),
                Player
            )
        )
        {
            OverrideDuration = AntiMainCampWarningTime
        };

        Player.SendToast(toast);
        if (_mainCampCoroutine != null)
        {
            Player.UnturnedPlayer.StopCoroutine(_mainCampCoroutine);
        }

        _mainCampCoroutine = Player.UnturnedPlayer.StartCoroutine(KillFromMainCamp());
    }

    private IEnumerator KillFromMainCamp()
    {
        DateTime now = DateTime.UtcNow;

        float timeToWait = AntiMainCampWarningTime;

        if (Player.Save.MainCampTime != DateTime.MinValue)
        {
            float timeMainCamping = (float)(now - Player.Save.MainCampTime).TotalSeconds;
            timeToWait = AntiMainCampWarningTime - timeMainCamping;
        }
        else
        {
            Player.Save.MainCampTime = now;
        }

        Player.Save.MainCampTime = now;

        if (timeToWait > 0)
            yield return new WaitForSecondsRealtime(timeToWait);

        if (!Player.IsOnline || Player.UnturnedPlayer.life.isDead || !CalculateIsMainCamping())
        {
            _isMainCamping = false;
            _mainCampCoroutine = null;
            Player.Save.MainCampTime = DateTime.MinValue;
            yield break;
        }

        Player.UnturnedPlayer.movement.forceRemoveFromVehicle();
        Player.UnturnedPlayer.life.askDamage(
            byte.MaxValue,
            Vector3.up / 8f,
            DeathTracker.InEnemyMainDeathCause,
            ELimb.SPINE,
            Player.Steam64,
            out _,
            trackKill: false,
            ERagdollEffect.NONE,
            canCauseBleeding: false,
            bypassSafezone: true
        );

        _mainCampCoroutine = null;
        _isMainCamping = false;
        Player.Save.MainCampTime = DateTime.MinValue;
        Player.Component<ToastManager>().SkipExpiration(ToastMessageStyle.FlashingWarning);
        // todo: ActionLog.Add(ActionLogType.MainCampAttempt, $"Player team: {Player.Team}, " +
        //                                              $"Location: {Player.Position.ToString("0.#", CultureInfo.InvariantCulture)}", Player);
    }

    private void CheckIsMainCamping()
    {
        if (CalculateIsMainCamping())
        {
            if (!_isMainCamping)
                SendMainCampWarning();

            return;
        }

        Player.Save.MainCampTime = DateTime.MinValue;
        if (!_isMainCamping)
        {
            return;
        }

        if (_mainCampCoroutine != null)
        {
            Player.UnturnedPlayer.StopCoroutine(_mainCampCoroutine);
            _mainCampCoroutine = null;
        }

        _isMainCamping = false;
        Player.Component<ToastManager>().SkipExpiration(ToastMessageStyle.FlashingWarning);
    }
}