using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.Seeding;

[UnturnedUI(BasePath = "Container/Box")]
internal class SeedingPlayHud : UnturnedUI, IEventListener<PlayerJoined>, IEventListener<PlayerLeft>, IHudUIListener
{
    private readonly SeedingPlayerCountMonitor _playerCountMonitor;
    private readonly ITranslationService _translationService;
    private readonly WarfareModule _module;
    private readonly SeedingTranslations _translations;
    private readonly HudManager? _hudManager;

    private bool _isEnabled;
    private bool _isHidden;

    private int _hiddenPlayers;

    public UnturnedLabel Title { get; } = new UnturnedLabel("Title");
    public UnturnedLabel Info { get; } = new UnturnedLabel("Info");
    public ImageProgressBar Progress { get; } = new ImageProgressBar("Progress") { NeedsToSetLabel = false };

    public SeedingPlayHudStage Stage
    {
        get
        {
            if (_playerCountMonitor is not { IsSeeding: true })
                return SeedingPlayHudStage.NotSeeding;
            
            return _playerCountMonitor.IsAwaitingStart
                ? SeedingPlayHudStage.WaitingForTimer
                : SeedingPlayHudStage.WaitingForPlayers;
        }
    }

    /// <inheritdoc />
    public SeedingPlayHud(
        IServiceProvider serviceProvider,
        SeedingPlayerCountMonitor playerCountMonitor,
        ITranslationService translationService,
        TranslationInjection<SeedingTranslations> translations,
        WarfareModule module)
        : base(
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            serviceProvider.GetRequiredService<AssetConfiguration>().GetAssetLink<EffectAsset>("UI:SeedingPlayHUD"),
            debugLogging: true
        )
    {
        _playerCountMonitor = playerCountMonitor;
        _translationService = translationService;
        _module = module;
        _hudManager = serviceProvider.GetService<HudManager>();
        _translations = translations.Value;
    }

    public void UpdateStage()
    {
        GameThread.AssertCurrent();
        GetLogger().LogInformation("Sending to all players...");

        if (_isHidden)
            return;

        string desc;
        SeedingPlayHudStage stage = Stage;
        GetLogger().LogInformation($"Stage: {stage}");
        switch (stage)
        {
            case SeedingPlayHudStage.WaitingForPlayers:
                desc = _translations.SeedingDescriptionWaitingForPlayers.Translate();
                break;

            case SeedingPlayHudStage.WaitingForTimer:
                desc = _translations.SeedingDescriptionWaitingForCooldown.Translate();
                break;

            default:
                if (!_isEnabled)
                    return;

                ClearFromAllPlayers();
                _isEnabled = false;
                _hudManager?.SetAllIsPluginVoting(false);
                return;
        }

        // must send separately if a player has HUD hidden
        string layoutName = _module.GetActiveLayout().LayoutInfo.DisplayName;
        LanguageSetEnumerator enumerator;
        bool sendToAll = _hiddenPlayers == 0;
        if (sendToAll)
        {
            SendToAllPlayers(layoutName, desc);
            enumerator = _translationService.SetOf.AllPlayers();
        }
        else
        {
            enumerator = _translationService.SetOf.PlayersWhere(p => !IsHidden(p));
        }

        _isEnabled = true;
        foreach (LanguageSet set in enumerator)
        {
            if (sendToAll && set.Language.IsDefault)
                continue;

            string desc2;
            if (!sendToAll && set.Language.IsDefault)
            {
                desc2 = desc;
            }
            else
            {
                desc2 = (stage == SeedingPlayHudStage.WaitingForPlayers
                        ? _translations.SeedingDescriptionWaitingForPlayers
                        : _translations.SeedingDescriptionWaitingForCooldown)
                    .Translate(set);
            }

            while (set.MoveNext())
            {
                if (!sendToAll)
                {
                    if (IsHidden(set.Next))
                        continue;

                    SendToPlayer(set.Next.Connection, layoutName, desc2);
                }
                else
                {
                    Info.SetText(set.Next, desc2);
                }
            }
        }

        UpdateProgress();
    }

    public void SendToPlayer(WarfarePlayer player)
    {
        GetLogger().LogInformation("Sending to player...");
        if (_isHidden || IsHidden(player))
            return;

        SeedingPlayHudStage stage = Stage;
        GetLogger().LogInformation($"Stage: {stage}.");
        Console.WriteLine(stage);
        string desc;
        switch (stage)
        {
            case SeedingPlayHudStage.WaitingForPlayers:
                desc = _translations.SeedingDescriptionWaitingForPlayers.Translate(player.Locale.LanguageInfo);
                break;

            case SeedingPlayHudStage.WaitingForTimer:
                desc = _translations.SeedingDescriptionWaitingForCooldown.Translate(player.Locale.LanguageInfo);
                break;

            default:
                ClearFromPlayer(player.Connection);
                _hudManager?.SetIsPluginVoting(player, false);
                return;
        }

        string layoutName = _module.GetActiveLayout().LayoutInfo.DisplayName;
        SendToPlayer(player.Connection, layoutName, desc);

        float p = GetProgressValue();
        string label = GetProgressLabel(player.Locale.CultureInfo);
        Progress.SetProgress(player.Connection, p);
        Progress.Label.SetText(player.Connection, label);
    }

    public void UpdateProgress()
    {
        GameThread.AssertCurrent();

        if (!_isEnabled || _isHidden)
            return;

        float p = GetProgressValue();

        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            string label = GetProgressLabel(set.Culture);
            while (set.MoveNext())
            {
                if (IsHidden(set.Next))
                    continue;

                Progress.SetProgress(set.Next.Connection, p);
                Progress.Label.SetText(set.Next, label);
            }
        }
    }

    private float GetProgressValue()
    {
        return Stage switch
        {
            SeedingPlayHudStage.WaitingForPlayers => (float)Provider.clients.Count
                                                     / _playerCountMonitor.Rules.StartPlayerThreshold,

            SeedingPlayHudStage.WaitingForTimer => (float)((_playerCountMonitor.AwaitDoneTime - DateTime.UtcNow).TotalSeconds
                                                           / _playerCountMonitor.Rules.StartCountdownLength.TotalSeconds),
            _ => 0
        };
    }

    private string GetProgressLabel(IFormatProvider cultureInfo)
    {
        string label;
        switch (Stage)
        {
            case SeedingPlayHudStage.WaitingForPlayers:
                int max = _playerCountMonitor.Rules.StartPlayerThreshold;
                label = $"{Provider.clients.Count.ToString(cultureInfo)}/{max.ToString(cultureInfo)}";
                break;

            case SeedingPlayHudStage.WaitingForTimer:
                TimeSpan timeLeft = _playerCountMonitor.AwaitDoneTime - DateTime.UtcNow;

                if (timeLeft.Ticks < 0)
                    timeLeft = TimeSpan.Zero;

                label = FormattingUtility.ToCountdownString(timeLeft, false);
                break;

            default:
                label = string.Empty;
                break;
        }

        return label;
    }

    private void SendFullToPlayer(WarfarePlayer player)
    {
        if (!_isEnabled || _isHidden || IsHidden(player))
            return;

        string desc = (Stage == SeedingPlayHudStage.WaitingForPlayers
                ? _translations.SeedingDescriptionWaitingForPlayers
                : _translations.SeedingDescriptionWaitingForCooldown)
            .Translate(player);

        SendToPlayer(player.Connection, _module.GetActiveLayout().LayoutInfo.DisplayName, desc);
        _hudManager?.SetIsPluginVoting(player, true);

        Progress.SetProgress(player.Connection, GetProgressValue());
        Progress.Label.SetText(player.Connection, GetProgressLabel(player.Locale.CultureInfo));
    }

    public enum SeedingPlayHudStage
    {
        NotSeeding,
        WaitingForPlayers,
        WaitingForTimer
    }

    [EventListener(Priority = -1)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (_isEnabled)
            SendFullToPlayer(e.Player);
    }

    [EventListener(MustRunInstantly = true, Priority = -1)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (IsHidden(e.Player))
            Interlocked.Decrement(ref _hiddenPlayers);
    }

    private bool IsHidden(WarfarePlayer player)
    {
        return GetData<SeedingPlayUIData>(player.Steam64) is { IsHidden: true };
    }

    private void SetHidden(WarfarePlayer player, bool isHidden)
    {
        if (IsHidden(player) == isHidden)
            return;

        GetOrAddData(player.Steam64, id => new SeedingPlayUIData(id, this)).IsHidden = isHidden;
        if (isHidden)
            Interlocked.Increment(ref _hiddenPlayers);
        else
            Interlocked.Decrement(ref _hiddenPlayers);
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            SetHidden(player, true);
            if (_isEnabled && !_isHidden)
                ClearFromPlayer(player.Connection);
            return;
        }

        ClearFromAllPlayers();
        _isHidden = true;
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            SetHidden(player, false);
            SendFullToPlayer(player);
            return;
        }

        _isHidden = false;
        UpdateStage();
        UpdateProgress();
    }
    public class SeedingPlayUIData(CSteamID player, UnturnedUI owner) : IUnturnedUIData
    {
        public bool IsHidden { get; set; }

        public CSteamID Player { get; } = player;
        public UnturnedUI Owner { get; } = owner;

        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}