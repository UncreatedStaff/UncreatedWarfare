using System;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using static Uncreated.Warfare.Layouts.UI.FlagListUI;

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
    private readonly HudManager _hudManager;

    public DefaultFlagListUIEvents(
        FlagListUI ui,
        IFlagListUIProvider uiProvider,
        ITicketTracker ticketTracker,
        ITranslationService translationService,
        Layout layout,
        HudManager hudManager)
    {
        _ui = ui;
        _uiProvider = uiProvider;
        _ticketTracker = ticketTracker;
        _translationService = translationService;
        _layout = layout;
        _hudManager = hudManager;
    }

    /// <inheritdoc />
    public void Hide(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.ClearFromPlayer(player);
            return;
        }

        _ui.ClearFromAllPlayers();
        foreach (FlagListUIData? data in UnturnedUIDataSource.Instance.EnumerateData(_ui).OfType<FlagListUIData>())
        {
            data.HasUI = false;
        }
    }

    /// <inheritdoc />
    public void Restore(WarfarePlayer? player)
    {
        if (player != null)
        {
            _ui.UpdateFlagList(_uiProvider, _ticketTracker, _layout.LayoutInfo.DisplayName, new LanguageSet(player));
            return;
        }

        UpdateFlagListForAllPlayers();
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
                        _layout.LayoutInfo.DisplayName,
                        new LanguageSet(set.Next),
                        ticketsOnly
                    );
                }
            }
        }
        else
        {
            _ui.UpdateFlagList(_uiProvider, _ticketTracker, _layout.LayoutInfo.DisplayName, set, ticketsOnly);
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