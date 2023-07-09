using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Warfare.Gamemodes;

namespace Uncreated.Warfare.Moderation;
public class ModerationUI : UnturnedUI
{
    public static ModerationUI Instance { get; } = new ModerationUI();

    /* HEADERS */
    public LabeledButton[] Headers { get; } =
    {
        new LabeledButton("ButtonModeration"),
        new LabeledButton("ButtonPlayers"),
        new LabeledButton("ButtonTickets"),
        new LabeledButton("ButtonLogs")
    };

    public UnturnedUIElement[] PageLogic { get; } =
    {
        new UnturnedUIElement("LogicPageModeration"),
        new UnturnedUIElement("LogicPagePlayers"),
        new UnturnedUIElement("LogicPageTickets"),
        new UnturnedUIElement("LogicPageLogs")
    };
    
    /* PLAYER LIST */
    public PlayerListEntry[] ModerationPlayerList { get; } = UnturnedUIPatterns.CreateArray<PlayerListEntry>("ModerationPlayer{1}_{0}", 1, to: 30);
    public UnturnedTextBox PlayerSearch { get; } = new UnturnedTextBox("ModerationPlayersInputSearch");
    public ChangeableTextBox ModerationPlayerSearch { get; } = new ChangeableTextBox("ModerationPlayersInputSearch");
    public UnturnedEnumButton<PlayerSearchMode> ModerationPlayerSearchModeButton { get; }
        = new UnturnedEnumButton<PlayerSearchMode>(PlayerSearchMode.Any, "ModerationButtonToggleOnline", "ModerationButtonToggleOnlineLabel")
        {
            TextFormatter = (v, player) => Localization.TranslateEnum(v, player.channel.owner.playerID.steamID.m_SteamID)
        };

    /* MODERATION HISTORY LIST */
    public ModerationHistoryEntry[] ModerationHistory { get; } = UnturnedUIPatterns.CreateArray<ModerationHistoryEntry>("ModerationEntry{1}_{0}", 1, to: 30);
    public LabeledStateButton ModerationHistoryBackButton { get; } = new LabeledStateButton("ModerationListBackButton");
    public LabeledStateButton ModerationHistoryNextButton { get; } = new LabeledStateButton("ModerationListNextButton");
    public ChangeableStateTextBox ModerationHistoryPage { get; } = new ChangeableStateTextBox("ModerationListPageInput");
    public ChangeableTextBox ModerationHistorySearch { get; } = new ChangeableTextBox("ModerationInputSearch");
    public LabeledButton ModerationResetHistory { get; } = new LabeledButton("ModerationResetHistory");
    public UnturnedEnumButton<ModerationEntryType> ModerationHistroyTypeButton { get; }
        = new UnturnedEnumButton<ModerationEntryType>(ModerationEntryType.None, "ModerationButtonToggleType", "ModerationButtonToggleTypeLabel")
        {
            TextFormatter = (v, player) => v == ModerationEntryType.None ? "Type - Any" : ("Type - " + Localization.TranslateEnum(v, player.channel.owner.playerID.steamID.m_SteamID))
        };
    public UnturnedEnumButton<ModerationHistorySearchMode> ModerationHistroySearchTypeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySearchMode>(ModerationHistorySearchMode.Message, "ModerationButtonToggleSearchMode", "ModerationButtonToggleSearchModeLabel")
        {
            TextFormatter = (v, player) => "Search - " + Localization.TranslateEnum(v, player.channel.owner.playerID.steamID.m_SteamID)
        };
    public UnturnedEnumButton<ModerationHistorySortMode> ModerationHistroySortTypeButton { get; }
        = new UnturnedEnumButton<ModerationHistorySortMode>(ModerationHistorySortMode.Latest, "ModerationButtonToggleSortType", "ModerationButtonToggleSortTypeLabel")
        {
            TextFormatter = (v, player) => "Sort - " + Localization.TranslateEnum(v, player.channel.owner.playerID.steamID.m_SteamID)
        };

    /* MODERATION HISTORY LIST */

    public ModerationUI() : base(Gamemode.Config.UIModerationMenu) { }

    public struct PlayerListEntry
    {
        [UIPattern("Name", Mode = FormatMode.Format)]
        public UnturnedLabel Name { get; set; }

        [UIPattern("ModerateButton", Mode = FormatMode.Format)]
        public UnturnedLabel ModerateButton { get; set; }

        [UIPattern("ModerateButtonLabel", Mode = FormatMode.Format)]
        public UnturnedLabel ModerateButtonLabel { get; set; }

        [UIPattern("SteamIDText", Mode = FormatMode.Format)]
        public UnturnedLabel SteamId { get; set; }

        [UIPattern("Pfp", Mode = FormatMode.Format)]
        public UnturnedImage ProfilePicture { get; set; }
    }
    public struct ModerationHistoryEntry
    {
        [UIPattern("Type", Mode = FormatMode.Format)]
        public UnturnedLabel Type { get; set; }

        [UIPattern("Reputation", Mode = FormatMode.Format)]
        public UnturnedLabel Reputation { get; set; }

        [UIPattern("Message", Mode = FormatMode.Format)]
        public UnturnedLabel Message { get; set; }

        [UIPattern("AdminPfp", Mode = FormatMode.Format)]
        public UnturnedImage AdminProfilePicture { get; set; }

        [UIPattern("Admin", Mode = FormatMode.Format)]
        public UnturnedLabel Admin { get; set; }

        [UIPattern("Timestamp", Mode = FormatMode.Format)]
        public UnturnedLabel Timestamp { get; set; }
    }

    public enum PlayerSearchMode
    {
        Any,
        Online
    }
    public enum ModerationHistorySearchMode
    {
        Admin,
        Message,
        Type,
        Before,
        After
    }
    public enum ModerationHistorySortMode
    {
        Latest,
        Oldest,
        [Translatable("Reputation (Highest)")]
        HighestReputation,
        [Translatable("Reputation (Lowest)")]
        LowestReputation,
        Type
    }
}
