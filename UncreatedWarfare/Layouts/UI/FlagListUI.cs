using System;
using System.Runtime.CompilerServices;
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

    public readonly UnturnedLabel TicketCount = new UnturnedLabel("TicketsNumber");
    public readonly UnturnedLabel TicketsFlagIcon = new UnturnedLabel("FactionFlagIcon");
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
        if (GetData<FlagListUIData>(player.Steam64) is not { HasUI: true } data)
            return;
        
        ApplyClear(player, data);
        ClearFromPlayer(player.UnturnedPlayer);
    }

    internal void ApplyClear(WarfarePlayer player, FlagListUIData data)
    {
        if (!data.HasUI)
            return;

        data.HasUI = false;
        data.ResetCache();
    }

    internal FlagListUIData GetOrAddData(WarfarePlayer player)
    {
        return GetOrAddData(player.Steam64, _getFlagListUIData);
    }

    private FlagListUIData GetFlagListUIData(CSteamID steam64)
    {
        return new FlagListUIData(steam64, this);
    }

    [SkipLocalsInit]
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
                ApplyClear(player, data);
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
                data.ResetCache();
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

                ref FlagRowInfo info = ref data.Cache[index];
                FlagElement element = Rows[index];
                ++index;

                FlagRowInfo newInfo;
                newInfo.Text = entry.Text;
                newInfo.Visible = true;

                if (string.IsNullOrEmpty(entry.Icon))
                {
                    newInfo.IconVisible = false;
                    newInfo.Icon = string.Empty;
                }
                else
                {
                    newInfo.IconVisible = true;
                    newInfo.Icon = entry.Icon;
                }

                info.UpdateDifferences(in newInfo, element, player);
            }

            for (int j = index; j < MaximumFlags; ++j)
            {
                ref FlagRowInfo info = ref data.Cache[j];
                if (!info.Visible)
                    break;

                info.Visible = false;
                Rows[j].Hide(connection);
            }
        }
    }

#nullable disable

    public class FlagElement : PatternRoot
    {
        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Icon")]
        public UnturnedLabel Icon { get; set; }
    }

#nullable restore

    internal class FlagListUIData : IUnturnedUIData
    {
        internal FlagRowInfo[] Cache = new FlagRowInfo[MaximumFlags];
        internal int Tickets;
        internal TicketBleedSeverity Bleed;
        internal bool HasUI;
        internal string? CustomTicket;
        internal FactionInfo? TicketsFlag;

        public CSteamID Player { get; }
        public UnturnedUI Owner { get; }
        public FlagListUIData(CSteamID player, UnturnedUI owner)
        {
            Player = player;
            Owner = owner;
        }

        internal void ResetCache()
        {
            for (int i = 0; i < Cache.Length; ++i)
                Cache[i].Reset(i);
        }
        UnturnedUIElement? IUnturnedUIData.Element => null;
    }

    private const string DefaultFlagText = "<#696969>unknown</color>";

    internal struct FlagRowInfo
    {
        public string Text;
        public string Icon;
        public bool IconVisible;
        public bool Visible;

        public void Reset(int index)
        {
            Text = DefaultFlagText;
            Icon = string.Empty;
            IconVisible = false;
            Visible = index <= 0;
        }

        public void UpdateDifferences(in FlagRowInfo data, FlagElement ui, WarfarePlayer player)
        {
            if (!data.Visible)
            {
                if (Visible)
                    ui.Hide(player);
                Visible = false;
                return;
            }

            if (!Visible)
            {
                ui.Show(player);
                Visible = true;
            }

            if (data.IconVisible && !string.Equals(data.Icon, Icon, StringComparison.Ordinal))
            {
                Icon = data.Icon;
                ui.Icon.SetText(player, data.Icon);
            }

            if (data.IconVisible != IconVisible)
            {
                IconVisible = data.IconVisible;
                ui.Icon.SetVisibility(player, IconVisible);
            }

            if (!string.Equals(data.Text, Text, StringComparison.OrdinalIgnoreCase))
            {
                Text = data.Text;
                ui.Name.SetText(player, Text);
            }
        }
    }
}