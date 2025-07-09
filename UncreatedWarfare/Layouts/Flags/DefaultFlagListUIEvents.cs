using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Flags;

/// <summary>
/// Default UI listeners for common Flag events.
/// This service should not be registered directly by the WarfareModule, but rather by each flag plugin.
/// </summary>
public class DefaultFlagListUIEvents :
    ILayoutStartingListener,
    IEventListener<IFlagsNeedUIUpdateEvent>,
    IHudUIListener
{
    private readonly FlagListUI _ui;
    private readonly IFlagListUIProvider _uiProvider;
    private readonly ITicketTracker _ticketTracker;
    private readonly ITranslationService _translationService;
    private readonly Layout _layout;

    public DefaultFlagListUIEvents(FlagListUI ui, IFlagListUIProvider uiProvider, ITicketTracker ticketTracker, ITranslationService translationService, Layout layout)
    {
        _ui = ui;
        _uiProvider = uiProvider;
        _ticketTracker = ticketTracker;
        _translationService = translationService;
        _layout = layout;
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.ClearFromPlayer(player);
            return;
        }

        _ui.IsHidden = true;
        UpdateFlagListForAllPlayers();
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.UpdateFlagList(_uiProvider, _ticketTracker, _layout.LayoutInfo.DisplayName, new LanguageSet(player));
            return;
        }

        _ui.IsHidden = false;
        UpdateFlagListForAllPlayers();
    }

    private void UpdateFlagList(LanguageSet set, bool ticketsOnly)
    {
        _ui.UpdateFlagList(_uiProvider, _ticketTracker, _layout.LayoutInfo.DisplayName, set, ticketsOnly);
    }

    private void UpdateFlagListForAllPlayers()
    {
        foreach (LanguageSet set in _translationService.SetOf.PlayersWhere(x => x.Team.IsValid))
        {
            UpdateFlagList(set, ticketsOnly: false);
        }
    }

    public UniTask HandleLayoutStartingAsync(Layout layout, CancellationToken token = default)
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
}