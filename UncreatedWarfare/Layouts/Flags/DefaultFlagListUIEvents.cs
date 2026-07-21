using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Flags;

/// <summary>
/// Default UI listeners for common Flag events.
/// This service should not be registered directly by <see cref="WarfareModule"/>, but rather by each flag plugin.
/// </summary>
public class DefaultFlagListUIEvents :
    ILayoutStartingListener,
    IEventListener<IFlagsNeedUIUpdateEvent>,
    IEventListener<PlayerLocaleUpdated>,
    IHudUIListener
{
    private readonly FlagListUI _ui;
    private readonly IFlagListUIProvider _uiProvider;
    private readonly ITicketTracker _ticketTracker;
    private readonly ITranslationService _translationService;
    private readonly IPlayerService _playerService;
    private readonly ITeamManager<Team> _teamManager;
    private readonly Layout _layout;
    private readonly HudManager _hudManager;

    public DefaultFlagListUIEvents(
        FlagListUI ui,
        IFlagListUIProvider uiProvider,
        ITicketTracker ticketTracker,
        ITranslationService translationService,
        IPlayerService playerService,
        ITeamManager<Team> teamManager,
        Layout layout,
        HudManager hudManager)
    {
        _ui = ui;
        _uiProvider = uiProvider;
        _ticketTracker = ticketTracker;
        _translationService = translationService;
        _playerService = playerService;
        _teamManager = teamManager;
        _layout = layout;
        _hudManager = hudManager;
    }

    private string GetLayoutDisplayName(Team team)
    {
        string displayName = _layout.LayoutInfo.DisplayName;
        if (!_layout.LayoutInfo.Configuration.DisplayRole || !team.IsValid)
            return "   " + displayName + "   ";

        LayoutRole role = _teamManager.GetLayoutRole(team);
        return role switch
        {
            LayoutRole.Opfor => "   " + displayName + "  <#ddd>µ</color>   ",
            LayoutRole.Blufor => "   " + displayName + "  <#ddd>´</color>   ",
            _ => "   " + displayName + "   "
        };
    }

    private void UpdateFlagList(LanguageSet set, bool ticketsOnly)
    {
        if (_hudManager.IsHiddenForAllPlayers)
            return;

        if (_hudManager.IsHiddenForAnyPlayers)
        {
            while (set.MoveNext())
            {
                if (!_hudManager.IsHidden(set.Next))
                {
                    _ui.UpdateFlagList(
                        _uiProvider,
                        _ticketTracker,
                        GetLayoutDisplayName(set.Team),
                        new LanguageSet(set.Next),
                        ticketsOnly
                    );
                }
            }
        }
        else
        {
            _ui.UpdateFlagList(_uiProvider, _ticketTracker, GetLayoutDisplayName(set.Team), set, ticketsOnly);
        }
    }

    private void UpdateFlagListForAllPlayers()
    {
        if (_hudManager.IsHiddenForAllPlayers)
            return;

        foreach (LanguageSet set in _translationService.SetOf.PlayersWhere(x => x.Team.IsValid))
        {
            UpdateFlagList(set, ticketsOnly: false);
        }
    }

    UniTask ILayoutStartingListener.HandleLayoutStartingAsync(Layout layout, CancellationToken token = default)
    {
        UpdateFlagListForAllPlayers();
        return UniTask.CompletedTask;
    }

    void IEventListener<IFlagsNeedUIUpdateEvent>.HandleEvent(IFlagsNeedUIUpdateEvent e, IServiceProvider serviceProvider)
    {
        bool ticketsOnly = false;
        foreach (LanguageSet set in e.EnumerateApplicableSets(_translationService, ref ticketsOnly))
        {
            UpdateFlagList(set, ticketsOnly);
        }
    }

    void IHudUIListener.Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.ClearFromPlayer(player);
            return;
        }

        _ui.ClearFromAllPlayers();
        foreach (WarfarePlayer p in _playerService.OnlinePlayers)
        {
            if (_ui.GetOrAddData(p) is { HasUI: true } data)
                _ui.ApplyClear(p, data);
        }
    }

    void IHudUIListener.Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.UpdateFlagList(_uiProvider, _ticketTracker, GetLayoutDisplayName(player.Team), new LanguageSet(player));
            return;
        }

        UpdateFlagListForAllPlayers();
    }

    void IEventListener<PlayerLocaleUpdated>.HandleEvent(PlayerLocaleUpdated e, IServiceProvider serviceProvider)
    {
        UpdateFlagList(new LanguageSet(e.Player), false);
    }
}