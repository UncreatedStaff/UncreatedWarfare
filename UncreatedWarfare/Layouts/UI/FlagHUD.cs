using Microsoft.Extensions.DependencyInjection;
using Stripe;
using System;
using System.Linq;
using System.Xml.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using static Uncreated.Warfare.Squads.UI.FobHUD;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "FlagHUD")]
public class FlagHUD : 
    UnturnedUI,
    IEventListener<PlayerTeamChanged>,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>,
    IEventListener<TicketsChanged>
{
    public readonly UnturnedLabel TicketCount = new UnturnedLabel("Tickets/TicketsNumber");
    public readonly UnturnedLabel TicketsFlagIcon = new UnturnedLabel("Tickets/FactionFlagIcon");
    public readonly UnturnedLabel GamemodeTitle = new UnturnedLabel("HeaderFlags");
    public readonly FlagElement[] Rows = ElementPatterns.CreateArray<FlagElement>("Flag_{0}/Flag{1}_{0}", 1, to: 10);
    public readonly Layout _layout;
    public readonly IFlagRotationService _flagService;
    public readonly ITicketTracker _ticketTracker;
    public readonly IPlayerService _playerService;
    public FlagHUD(IServiceProvider serviceProvider, AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FlagHUD"))
    {
        _layout = serviceProvider.GetRequiredService<Layout>();
        _flagService = serviceProvider.GetRequiredService<IFlagRotationService>();
        _ticketTracker = serviceProvider.GetRequiredService<ITicketTracker>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
    }
    public void HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        if (!e.Team.IsValid)
        {
            ClearFromPlayer(e.Player.Connection);
            return;
        }

        SendToPlayer(e.Player.Connection);
        UpdateFlagList(e.Player);
        UpdateTicketCount(e.Player, _ticketTracker.GetTickets(e.Team), _ticketTracker.GetBleedSeverity(e.Team).GetBleedMessage());
    }
    void IEventListener<FlagCaptured>.HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        UpdateFlagListAllPlayers();
    }

    void IEventListener<FlagNeutralized>.HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        UpdateFlagListAllPlayers();
    }
    void IEventListener<TicketsChanged>.HandleEvent(TicketsChanged e, IServiceProvider serviceProvider)
    {
        UpdateTicketCountForPlayersOnTeam(e.Team, e.NewNumber, e.TicketTracker.GetBleedSeverity(e.Team).GetBleedMessage());
    }
    void UpdateTicketCount(WarfarePlayer player, int tickets, string ticketBleedDescription)
    {
        TicketsFlagIcon.SetText(player, player.Team.Faction.Sprite);

        string ticketsMessage = tickets.ToString(player.Locale.CultureInfo);
        if (!string.IsNullOrEmpty(ticketBleedDescription))
            ticketsMessage += "  " + TranslationFormattingUtility.Colorize(ticketBleedDescription, "#e88e8e");

        TicketCount.SetText(player, ticketsMessage);
    }
    void UpdateTicketCountForPlayersOnTeam(Team team, int tickets, string ticketBleedDescription)
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
            UpdateTicketCount(player, tickets, ticketBleedDescription);
    }
    void UpdateFlagList(WarfarePlayer player)
    {
        GamemodeTitle.SetText(player, _layout.LayoutInfo.DisplayName);

        FlagObjective? currentObjective = _flagService.GetObjective(player.Team);

        for (int i = 0; i < _flagService.ActiveFlags.Count; i++)
        {
            if (i >= _flagService.ActiveFlags.Count)
                break;

            FlagObjective flag = _flagService.ActiveFlags[i];
            
            FlagElement element = Rows[i];
            element.Root.Show(player);
            element.Name.SetText(player, TranslationFormattingUtility.Colorize(flag.Name, flag.Owner.Faction.Color));
            
            if (currentObjective != null && currentObjective == flag)
            {
                element.Icon.SetText(player, TranslationFormattingUtility.Colorize("µ", "ff8963"));
            }
            else
                element.Icon.SetText(player, string.Empty);
        }
    }
    void UpdateFlagListAllPlayers()
    {
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            UpdateFlagList(player);
    }
    public class FlagElement
    {
        [Pattern("", Root = true, CleanJoin = '_')]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [Pattern("Icon", Mode = FormatMode.Format)]
        public UnturnedLabel Icon { get; set; }
    }
}
