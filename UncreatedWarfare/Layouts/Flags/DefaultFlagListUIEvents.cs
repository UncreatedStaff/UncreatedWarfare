using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Tickets;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.Flags;

public class DefaultFlagListUIEvents :
    IEventListener<PlayerTeamChanged>,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>,
    IEventListener<TicketsChanged>
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

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        UpdateFlagList(new LanguageSet(e.Player), ticketsOnly: false);
    }

    void IEventListener<FlagCaptured>.HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        UpdateFlagListForAllPlayers();
    }

    void IEventListener<FlagNeutralized>.HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        UpdateFlagListForAllPlayers();
    }
    void IEventListener<TicketsChanged>.HandleEvent(TicketsChanged e, IServiceProvider serviceProvider)
    {
        foreach (LanguageSet set in _translationService.SetOf.PlayersOnTeam(e.Team))
        {
            UpdateFlagList(set, ticketsOnly: true);
        }
    }
}
