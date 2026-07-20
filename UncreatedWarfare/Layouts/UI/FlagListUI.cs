using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Tickets;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Layouts.UI;

[UnturnedUI(BasePath = "FlagHUD")]
public class FlagListUI : UnturnedUI
{
    /// <summary>
    /// Maximum number of flags that can be displayed on the flag list.
    /// </summary>
    public static readonly int MaximumFlags = 12;

    private readonly FlagUITranslations _translations;
    private readonly Func<CSteamID, FlagListUIData> _getFlagListUIData;

    public readonly UnturnedLabel TicketCount = new UnturnedLabel("Tickets/TicketsNumber");
    public readonly UnturnedLabel TicketsFlagIcon = new UnturnedLabel("Tickets/FactionFlagIcon");
    public readonly UnturnedLabel GamemodeTitle = new UnturnedLabel("GamemodeName");
    public readonly FlagElement[] Rows = ElementPatterns.CreateArray<FlagElement>("Flag_{0}", 0, MaximumFlags);

    public FlagListUI(TranslationInjection<FlagUITranslations> translations, AssetConfiguration assetConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FlagHUD"), reliable: false)
    {
        IsSendReliable = true;

        _translations = translations.Value;

        _getFlagListUIData = GetFlagListUIData;
    }

    public void ClearFromPlayer(WarfarePlayer player)
    {
        ClearFromPlayer(player.UnturnedPlayer);
        GetOrAddData(player).HasUI = false;
    }

    internal FlagListUIData GetOrAddData(WarfarePlayer player)
    {
        return GetOrAddData(player.Steam64, _getFlagListUIData);
    }

    private FlagListUIData GetFlagListUIData(CSteamID steam64)
    {
        return new FlagListUIData(steam64, this);
    }

    public void UpdateFlagList(IFlagListUIProvider flagProvider, ITicketTracker ticketTracker, string layoutName, LanguageSet set, bool ticketsOnly = false)
    {
        // hide UI for invalid teams
        if (!set.Team.IsValid)
        {
            while (set.MoveNext())
            {
                WarfarePlayer player = set.Next;
                FlagListUIData data = GetOrAddData(player);
                if (!data.HasUI)
                    continue;

                ClearFromPlayer(player.Connection);
                data.HasUI = false;
            }

            return;
        }

        FactionInfo faction = set.Team.Faction;
        int tickets = ticketTracker.GetTickets(set.Team);

        ICustomUITicketTracker? customUiTracker = ticketTracker as ICustomUITicketTracker;

        bool playerSpecific = customUiTracker is { PlayerSpecific: true };

        bool hasTicketInfo = false;
        string? ticketsStr = null;
        TicketBleedSeverity bleed = 0;
        FactionInfo? ticketFaction = null;


        while (set.MoveNext())
        {
            bool ticketsOnlyForThisPlayer = ticketsOnly;
            WarfarePlayer player = set.Next;
            ITransportConnection connection = player.Connection;
            FlagListUIData data = GetOrAddData(player);
            if (!data.HasUI)
            {
                SendToPlayer(connection);
                data.HasUI = true;
                data.Tickets = int.MinValue;
                data.CustomTicket = null;
                data.Bleed = TicketBleedSeverity.None;
                data.TicketsFlag = null;
                data.Rows = 1;
                GamemodeTitle.SetText(player, layoutName);
                ticketsOnlyForThisPlayer = false;
            }

            // update ticket text
            bool fallback = true;
            if (customUiTracker != null)
            {
                if (playerSpecific || !hasTicketInfo)
                {
                    (ticketsStr, ticketFaction) = customUiTracker.GetTicketText(playerSpecific ? new LanguageSet(player) : set, out fallback);
                    if (fallback)
                    {
                        if (!playerSpecific)
                            customUiTracker = null;
                        hasTicketInfo = false;
                    }
                    else
                    {
                        hasTicketInfo = true;
                    }
                }

                if (!fallback)
                {
                    if (!string.Equals(data.CustomTicket, ticketsStr, StringComparison.Ordinal))
                        TicketCount.SetText(player.Connection, ticketsStr!);
                    data.CustomTicket = ticketsStr;
                }
            }

            // fallback to default tickets
            if (fallback)
            {
                if (!hasTicketInfo)
                {
                    bleed = ticketTracker.GetBleedSeverity(set.Team);

                    ticketsStr = tickets.ToString(set.Culture);

                    string bleedMessage = _translations.GetBleedMessage(bleed).Translate(in set);
                    if (!string.IsNullOrEmpty(bleedMessage))
                        ticketsStr += "  " + bleedMessage;

                    ticketFaction = faction;
                    hasTicketInfo = true;
                }

                if (data.Tickets != tickets || data.Bleed != bleed || data.CustomTicket != null)
                {
                    TicketCount.SetText(connection, ticketsStr!);
                    data.Tickets = tickets;
                    data.Bleed = bleed;
                    data.CustomTicket = null;
                }
            }

            if (!Equals(data.TicketsFlag, ticketFaction))
            {
                TicketsFlagIcon.SetText(connection, ticketFaction!.Sprite);
                data.TicketsFlag = ticketFaction;
            }

            if (ticketsOnlyForThisPlayer)
                continue;

            // update flags
            int index = 0;
            foreach (FlagListUIEntry entry in flagProvider.EnumerateFlagListEntries(set))
            {
                if (index >= MaximumFlags)
                    break;

                FlagElement element = Rows[index];
                ++index;

                if (index >= data.Rows)
                    element.Show(connection);

                element.Name.SetText(connection, entry.Text);
                if (string.IsNullOrEmpty(entry.Icon))
                {
                    element.Icon.Hide(connection);
                }
                else
                {
                    element.Icon.SetText(connection, entry.Text);
                    element.Icon.Show(connection);
                }
            }

            for (int j = index; j < data.Rows; ++j)
            {
                Rows[j].Root.Hide(connection);
            }
            data.Rows = index;
        }
    }

#nullable disable

    public class FlagElement : PatternRoot
    {
        [Pattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [Pattern("Icon", Mode = FormatMode.Format)]
        public UnturnedLabel Icon { get; set; }
    }

#nullable restore

    internal class FlagListUIData : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        public bool HasUI { get; set; }
        public FactionInfo? TicketsFlag { get; set; }
        public int Tickets { get; set; }
        public TicketBleedSeverity Bleed { get; set; }
        public int Rows { get; set; } = 1;
        public string? CustomTicket { get; set; }
        public FlagListUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }

        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}