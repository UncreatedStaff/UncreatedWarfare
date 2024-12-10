using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

[UnturnedUI(BasePath = "Container")]
public partial class DualSidedLeaderboardUI : UnturnedUI, ILeaderboardUI, IEventListener<PlayerJoined>, IEventListener<PlayerLeft>, ILayoutPhaseListener<ILayoutPhase>, IDisposable
{
    [Ignore]
    private LeaderboardSet[]? _sets;

    [Ignore]
    private DateTime _startTimestamp;

    [Ignore]
    private readonly Layout _layout;

    private readonly ChatService _chatService;
    private readonly IPlayerService _playerService;
    private readonly ITranslationService _translationService;
    private readonly Func<CSteamID, DualSidedLeaderboardPlayerData> _createData;

    public readonly TopSquad[] TopSquads = ElementPatterns.CreateArray<TopSquad>("GameInfo/Squads/Squad_T{0}", 1, to: 2);

    public readonly UnturnedLabel TopSquadsTitle = new UnturnedLabel("GameInfo/Squads/Title");

    public readonly LeaderboardList[] Leaderboards = ElementPatterns.CreateArray<LeaderboardList>("LbParent/T{0}", 1, to: 2);

    public readonly UnturnedLabel LayoutName = new UnturnedLabel("GameInfo/LayoutName");
    public readonly UnturnedLabel Countdown = new UnturnedLabel("GameInfo/Timer");

    public readonly UnturnedLabel WinnerTeamName = new UnturnedLabel("GameInfo/Stats/Global/WinnerTitle");
    public readonly UnturnedLabel GameDuration = new UnturnedLabel("GameInfo/Stats/Global/Duration");
    public readonly UnturnedLabel WinnerFlag = new UnturnedLabel("GameInfo/Stats/Global/Flag");
    public readonly UnturnedLabel[] GlobalStats = ElementPatterns.CreateArray<UnturnedLabel>("GameInfo/Stats/Global/TeamStats/Viewport/Content/Stat_{0}", 0, to: 4);
    public readonly ValuablePlayer[] ValuablePlayers = ElementPatterns.CreateArray<ValuablePlayer>("GameInfo/Stats/Global/MVPs/Viewport/Content/Recognized_{0}", 1, to: 6);

    public readonly UnturnedLabel PointsExperienceGained = new UnturnedLabel("GameInfo/Stats/Player/XP");
    public readonly UnturnedLabel PointsCreditsGained = new UnturnedLabel("GameInfo/Stats/Player/Credits");
    public readonly ImageProgressBar PointsProgressBar = new ImageProgressBar("GameInfo/Stats/Player/LevelProgress");
    public readonly UnturnedUIElement PointsUpgradeArrow = new UnturnedUIElement("GameInfo/Stats/Player/RankUpgrade");
    public readonly UnturnedUIElement PointsDowngradeArrow = new UnturnedUIElement("GameInfo/Stats/Player/RankDowngrade");
    public readonly UnturnedLabel PointsCurrentRank = new UnturnedLabel("GameInfo/Stats/Player/RankCurrent");
    public readonly UnturnedLabel PointsNextRank = new UnturnedLabel("GameInfo/Stats/Player/RankNext");
    
    public bool IsActive { get; private set; }

    public DualSidedLeaderboardUI(AssetConfiguration assetConfig, ILoggerFactory loggerFactory, Layout layout, ChatService chatService, IPlayerService playerService, ITranslationService translationService)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:DualSidedLeaderboardUI"), staticKey: true, debugLogging: true)
    {
        _createData = CreateData;
        _layout = layout;
        _chatService = chatService;
        _playerService = playerService;
        _translationService = translationService;

        ElementPatterns.SubscribeAll(Leaderboards[0].StatHeaders, Team1ButtonPressed);
        ElementPatterns.SubscribeAll(Leaderboards[1].StatHeaders, Team2ButtonPressed);
    }

    private void Team1ButtonPressed(UnturnedButton button, Player player)
    {
        int ind = Array.IndexOf(Leaderboards[0].StatHeaders, button);
        if (ind == -1)
            return;

        UpdateSort(_playerService.GetOnlinePlayer(player), 0, ind);
    }

    private void Team2ButtonPressed(UnturnedButton button, Player player)
    {
        int ind = Array.IndexOf(Leaderboards[1].StatHeaders, button);
        if (ind == -1)
            return;

        UpdateSort(_playerService.GetOnlinePlayer(player), 1, ind);
    }

    public void Open(LeaderboardSet[] sets)
    {
        GameThread.AssertCurrent();

        if (IsActive)
            return;

        _startTimestamp = DateTime.UtcNow;

        if (sets.Length != 2)
            throw new ArgumentException("DualSidedLeaderboardUI only accepts two teams.", nameof(sets));

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            player.UnturnedPlayer.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            if (player.ComponentOrNull<AudioRecordPlayerComponent>() is { } comp)
            {
                comp.VoiceChatStateUpdated += CompOnVoiceChatStateUpdated;
            }
        }

        _sets = sets;
        SendToAllPlayers();
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            SendToPlayers(set);
        }

        OpenChat();
        IsActive = true;
    }

    public void Close()
    {
        GameThread.AssertCurrent();

        if (!IsActive)
            return;

        IsActive = false;
        ClearFromAllPlayers();
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            player.UnturnedPlayer.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            if (player.IsOnline && player.ComponentOrNull<AudioRecordPlayerComponent>() is { } comp)
                comp.VoiceChatStateUpdated -= CompOnVoiceChatStateUpdated;
        }
    }

    private void SendToPlayers(LanguageSet set)
    {
        Team? winningTeam = _layout.Data[KnownLayoutDataKeys.WinnerTeam] as Team;
        while (set.MoveNext())
        {
            LayoutName.SetText(set.Next.Connection, _layout.LayoutInfo.DisplayName);
        }

        set.Reset();

        if (winningTeam != null)
        {
            string teamSprite = winningTeam.Faction.Sprite;
            string teamName = winningTeam.Faction.GetName(set.Language);

            string countdown = FormattingUtility.ToCountdownString(_startTimestamp - _layout.LayoutStats.StartTimestamp.UtcDateTime, true);

            while (set.MoveNext())
            {
                ITransportConnection c = set.Next.Connection;
                WinnerFlag.SetText(c, teamSprite);
                WinnerTeamName.SetText(c, teamName);
                GameDuration.SetText(c, countdown);

                // todo team stats
            }

            set.Reset();
        }

        if (_sets == null)
            return;

        for (int i = 0; i < _sets.Length; ++i)
        {
            SendLeaderboard(i, set, true);
        }
    }

    public void UpdateSort(WarfarePlayer player, int setIndex, int column)
    {
        DualSidedLeaderboardPlayerData data = GetOrAddData(player.Steam64, _createData);
        ref LeaderboardSortColumn sort = ref data.SortColumns[setIndex];

        if (sort.ColumnIndex == column)
        {
            sort.Descending = !sort.Descending;
        }
        else
        {
            sort.Descending = true;
            sort.ColumnIndex = column;
        }

        if (_sets == null)
            return;

        LanguageSet langSet = new LanguageSet(player);
        for (int i = 0; i < _sets.Length; ++i)
        {
            SendLeaderboard(i, langSet, false);
        }
    }

    private void SendLeaderboard(int setIndex, LanguageSet langSet, bool team)
    {
        LeaderboardSet set = _sets![setIndex];

        LeaderboardList uiList = Leaderboards[setIndex];

        if (team)
        {
            string sprite = set.Team.Faction.Sprite;
            string name = set.Team.Faction.GetName(langSet.Language);

            while (langSet.MoveNext())
            {
                ITransportConnection c = langSet.Next.Connection;
                uiList.TeamFlag.SetText(c, sprite);
                uiList.TeamName.SetText(c, name);
            }
        }

        LeaderboardSet.LeaderboardRow[] rows = set.Rows;

        GetLogger().LogInformation("{0} Rows: {1}", setIndex, rows.Length);

        int statCount = set.ColumnCount;
        for (int i = 0; i < rows.Length; ++i)
        {
            ref LeaderboardSet.LeaderboardRow row = ref rows[i];
            LeaderboardPlayer player = row.Player;

            Span<string> formats = row.FormatData(langSet.Culture);

            langSet.Reset();
            GetLogger().LogInformation("{0} row: {1}. pl: {2}", setIndex, i, player.Player);

            while (langSet.MoveNext())
            {
                if (!TryMapRowToUIRow(setIndex, i, GetOrAddData(langSet.Next.Steam64, _createData), out LeaderboardPlayerRow? uiRow))
                {
                    GetLogger().LogInformation("{0} row: {1} no map", setIndex, i);
                    continue;
                }

                ITransportConnection c = langSet.Next.Connection;
                uiRow.Avatar.SetImage(c, player.Player.SteamSummary.AvatarUrlSmall ?? string.Empty);
                uiRow.PlayerName.SetText(c, player.Player.Names.CharacterName);

                for (int s = 0; s < statCount; ++s)
                    uiRow.Stats[s].SetText(c, formats[s]);

                // voice chat icon
                uiRow.VoiceChatState.SetVisibility(c, player.Player.IsOnline && player.Player.ComponentOrNull<AudioRecordPlayerComponent>() is { RecentlyUsedVoiceChat: false });
                uiRow.Root.SetVisibility(c, true);
            }
        }
    }

    private bool TryMapRowToUIRow(int set, int row, DualSidedLeaderboardPlayerData data, [MaybeNullWhen(false)] out LeaderboardPlayerRow uiRow)
    {
        // gets the sorted row UI position for a row index from the original data table for a player
        ref LeaderboardSortColumn sort = ref data.SortColumns[set];
        int[] invSortMap = _sets![set].GetSortMap(sort.ColumnIndex, sort.Descending);
        LeaderboardList list = Leaderboards[set];

        int uiIndex = invSortMap[row];
        if (uiIndex >= list.Players.Length)
        {
            uiRow = null;
            return false;
        }

        uiRow = list.Players[uiIndex];
        return true;
    }

    private void CompOnVoiceChatStateUpdated(WarfarePlayer player, bool isUsingVoiceChat)
    {
        if (_sets == null)
            return;

        for (int i = 0; i < _sets.Length; ++i)
        {
            LeaderboardSet set = _sets[i];
            int rowInd = set.GetRowIndex(player.Steam64);
            if (rowInd < 0)
                continue;

            foreach (WarfarePlayer onlinePlayer in _playerService.OnlinePlayers)
            {
                if (!TryMapRowToUIRow(i, rowInd, GetOrAddData(onlinePlayer.Steam64, _createData), out LeaderboardPlayerRow? uiRow))
                    continue;

                uiRow.VoiceChatState.SetVisibility(onlinePlayer.Connection, isUsingVoiceChat);
            }
        }
    }

    public void UpdateCountdown(TimeSpan timeLeft)
    {
        string countdown = FormattingUtility.ToCountdownString(timeLeft, false);
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            Countdown.SetText(player.Connection, countdown);
        }
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (!IsActive)
            return;

        if (e.Player.ComponentOrNull<AudioRecordPlayerComponent>() is { } comp)
        {
            comp.VoiceChatStateUpdated += CompOnVoiceChatStateUpdated;
        }

        SendToPlayers(new LanguageSet(e.Player));
        UpdateChat(e.Player);
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (!IsActive)
            return;

        if (e.Player.ComponentOrNull<AudioRecordPlayerComponent>() is { } comp)
        {
            comp.VoiceChatStateUpdated -= CompOnVoiceChatStateUpdated;
        }
    }

    UniTask ILayoutPhaseListener<ILayoutPhase>.OnPhaseStarted(ILayoutPhase phase, CancellationToken token)
    {
        if (_trackChat || _layout.NextPhase is not LeaderboardPhase)
            return UniTask.CompletedTask;

        _trackChat = true;
        _chatService.OnSendingChatMessage += ChatServiceOnOnSendingChatMessage;
        return UniTask.CompletedTask;
    }

    UniTask ILayoutPhaseListener<ILayoutPhase>.OnPhaseEnded(ILayoutPhase phase, CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    void IDisposable.Dispose()
    {
        if (_trackChat)
        {
            _chatService.OnSendingChatMessage -= ChatServiceOnOnSendingChatMessage;
            _trackChat = false;
        }
        Dispose();
    }

    private DualSidedLeaderboardPlayerData CreateData(CSteamID steam64)
    {
        return new DualSidedLeaderboardPlayerData(steam64, this);
    }

#nullable disable

    public class TopSquad
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Flag")]
        public UnturnedLabel Flag { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Stat")]
        public UnturnedLabel ImportantStatistic { get; set; }

        [Pattern("Member_{0}", AdditionalPath = "ScrollBox/Viewport/Content")]
        [ArrayPattern(0, To = 5)]
        public UnturnedLabel[] Members { get; set; }
    }

    public class LeaderboardList
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Header/Team/Text")]
        public UnturnedLabel TeamName { get; set; }

        [Pattern("Header/Team/Avatar")]
        public UnturnedLabel TeamFlag { get; set; }

        [ArrayPattern(1, To = 6)]
        [Pattern("Header/Lb_T{1}_Hdr_{0}")]
        public UnturnedButton[] StatHeaders { get; set; }

        [ArrayPattern(1, To = 50)]
        [Pattern("ScrollBox/Viewport/Content/Player_{0}")]
        public LeaderboardPlayerRow[] Players { get; set; }
    }

    public class LeaderboardPlayerRow
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Player/Text")]
        public UnturnedLabel PlayerName { get; set; }

        [Pattern("Player/AvatarMask/Avatar")]
        public UnturnedImage Avatar { get; set; }

        [Pattern("Player/VC")]
        public UnturnedUIElement VoiceChatState { get; set; }

        [ArrayPattern(1, To = 6)]
        [Pattern("Stat_{0}/Value")]
        public UnturnedLabel[] Stats { get; set; }
    }

    public class ValuablePlayer
    {
        [Pattern(Root = true)]
        public UnturnedUIElement Root { get; set; }

        [Pattern("Name")]
        public UnturnedLabel Name { get; set; }

        [Pattern("Label")]
        public UnturnedLabel Role { get; set; }

        [Pattern("AvatarMask/Avatar")]
        public UnturnedImage Avatar { get; set; }

        [Pattern("Value")]
        public UnturnedLabel Value { get; set; }
    }

#nullable restore
}