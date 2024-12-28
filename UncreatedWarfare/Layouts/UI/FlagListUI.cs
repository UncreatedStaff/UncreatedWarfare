using SDG.NetTransport;
using System;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Data;
using Uncreated.Framework.UI.Patterns;
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
    private readonly FlagUITranslations _translations;
    private readonly Func<CSteamID, FlagListUIData> _getFlagListUIData;

    public readonly UnturnedLabel TicketCount = new UnturnedLabel("Tickets/TicketsNumber");
    public readonly UnturnedLabel TicketsFlagIcon = new UnturnedLabel("Tickets/FactionFlagIcon");
    public readonly UnturnedLabel GamemodeTitle = new UnturnedLabel("HeaderFlags");
    public readonly FlagElement[] Rows = ElementPatterns.CreateArray<FlagElement>("Flag_{0}/Flag{1}_{0}", 1, to: 10);
    
    public FlagListUI(TranslationInjection<FlagUITranslations> translations, AssetConfiguration assetConfig, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:FlagHUD"), reliable: false)
    {
        IsSendReliable = true;

        _translations = translations.Value;

        _getFlagListUIData = GetFlagListUIData;
    }
    private FlagListUIData GetOrAddData(WarfarePlayer player)
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

        TicketBleedSeverity bleed = ticketTracker.GetBleedSeverity(set.Team);

        string ticketsStr = tickets.ToString(set.Culture);

        string bleedMessage = _translations.GetBleedMessage(bleed).Translate(set);
        if (!string.IsNullOrEmpty(bleedMessage))
            ticketsStr += "  " + bleedMessage;

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
                data.Bleed = TicketBleedSeverity.None;
                data.TicketsFlag = null;
                GamemodeTitle.SetText(player, layoutName);
                ticketsOnlyForThisPlayer = false;
            }

            if (data.Tickets != tickets || data.Bleed != bleed)
            {
                TicketCount.SetText(connection, ticketsStr);
                data.Tickets = tickets;
                data.Bleed = bleed;
            }

            if (data.TicketsFlag != faction)
            {
                TicketsFlagIcon.SetText(connection, faction.Sprite);
                data.TicketsFlag = faction;
            }

            if (ticketsOnlyForThisPlayer)
                continue;

            int index = 0;
            foreach (FlagListUIEntry entry in flagProvider.EnumerateFlagListEntries(set))
            {
                if (index >= Rows.Length)
                    break;

                FlagElement element = Rows[index];
                ++index;

                if (index >= data.Rows)
                    element.Root.SetVisibility(connection, true);

                element.Name.SetText(connection, entry.Text);
                element.Icon.SetText(connection, entry.Icon);
            }

            data.Rows = index;
            for (int j = data.Rows - 1; j >= index; ++j)
            {
                Rows[j].Root.SetVisibility(connection, false);
            }
        }
    }

#nullable disable

    public class FlagElement
    {
        [Pattern("", Root = true, CleanJoin = '_')]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [Pattern("Icon", Mode = FormatMode.Format)]
        public UnturnedLabel Icon { get; set; }
    }

#nullable restore

    private class FlagListUIData : IUnturnedUIData
    {
        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        public bool HasUI { get; set; }
        public FactionInfo? TicketsFlag { get; set; }
        public int Tickets { get; set; }
        public TicketBleedSeverity Bleed { get; set; }
        public int Rows { get; set; }
        public FlagListUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }

        UnturnedUIElement? IUnturnedUIData.Element => null;
    }
}