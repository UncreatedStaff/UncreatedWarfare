using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Patterns;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.UI;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts.UI.Leaderboards;

[UnturnedUI(BasePath = "Container")]
public partial class DualSidedLeaderboardUI : UnturnedUI, ILeaderboardUI, IEventListener<PlayerJoined>, IEventListener<PlayerLeft>, ILayoutPhaseListener<ILayoutPhase>, IDisposable
{
    [Ignore] private LeaderboardSet[]? _sets;
    [Ignore] private List<PlayerGlobalStat>? _globalStats;

    [Ignore] private DateTime _startTimestamp;

    [Ignore] private readonly Layout _layout;

    [Ignore] private readonly ChatService _chatService;
    [Ignore] private readonly IPlayerService _playerService;
    [Ignore] private readonly SquadManager _squadManager;
    [Ignore] private readonly PointsService _pointsService;
    [Ignore] private readonly ITranslationService _translationService;
    [Ignore] private readonly Func<CSteamID, DualSidedLeaderboardPlayerData> _createData;

    public readonly UnturnedUIElement TopSquadsParent = new UnturnedUIElement("GameInfo/Squads");
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

    public DualSidedLeaderboardUI(
        AssetConfiguration assetConfig,
        ILoggerFactory loggerFactory,
        Layout layout,
        ChatService chatService,
        IPlayerService playerService,
        SquadManager squadManager,
        PointsService pointsService,
        ITranslationService translationService)

        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:DualSidedLeaderboardUI"), staticKey: true, debugLogging: false)
    {
        _createData = CreateData;
        _layout = layout;
        _chatService = chatService;
        _playerService = playerService;
        _squadManager = squadManager;
        _pointsService = pointsService;
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
        _globalStats = ComputeGlobalStats();
        
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
            SendPointsSection(set.Next);
        }
        set.Reset();

        SendTopSquads(set);
        SendGlobalStats(set);

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

                // todo: game stats
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

    private void SendTopSquads(LanguageSet set)
    {
        // no squads or at least one team has no squads
        if (_squadManager.Squads.Count == 0 || _layout.TeamManager.AllTeams.Any(x => _squadManager.Squads.All(y => y.Team != x)))
        {
            while (set.MoveNext())
                TopSquadsParent.SetVisibility(set.Next.Connection, false);
            return;
        }

        while (set.MoveNext())
            TopSquadsParent.SetVisibility(set.Next.Connection, true);

        for (int i = 0; i < _sets!.Length; i++)
        {
            LeaderboardSet leaderboardSet = _sets[i];
            
            int xpStatIndex = leaderboardSet.GetStatisticIndex(KnownStatNames.XP);
            Squad topSquad = _squadManager.Squads.Aggregate((s1, s2) =>
            {
                double s1AverageScore = s1.Members.Sum(m => leaderboardSet.GetStatisticValue(xpStatIndex, m.Steam64)) / s1.Members.Count;
                double s2AverageScore = s2.Members.Sum(m => leaderboardSet.GetStatisticValue(xpStatIndex, m.Steam64)) / s2.Members.Count;
                return s1AverageScore > s2AverageScore ? s1 : s2;
            });

            TopSquad squadElement = TopSquads[i];
            
            double totalSquadXP = topSquad.Members.Sum(m => leaderboardSet.GetStatisticValue(xpStatIndex, m.Steam64));
            string totalSquadXPWithHeader = $"Squad XP: <#ccffd4>{totalSquadXP.ToString("F0", set.Culture)}</color>";

            set.Reset();
            while (set.MoveNext())
            {
                squadElement.Name.SetText(set.Next.Connection, topSquad.Name);
                squadElement.Flag.SetText(set.Next.Connection, topSquad.Team.Faction.Sprite);
                
                for (int m = 0; m < topSquad.Members.Count; m++)
                {
                    if (m >= squadElement.Members.Length)
                        break;
                    
                    WarfarePlayer member = topSquad.Members[m];
                    UnturnedLabel memberElement = squadElement.Members[m];
                    char classIcon = member.Component<KitPlayerComponent>().ActiveClass.GetIcon();
                    string rank = _pointsService.GetRankFromExperience(member.CachedPoints.XP).Abbreviation;
                    string memberName = $"{rank} {member.Names.CharacterName}";
                    if (member.IsSquadLeader())
                        memberName = $"<#ccffd4>{memberName}</color>";
                    memberElement.SetText(set.Next.Connection, $"<mspace=2em>{classIcon}</mspace> {memberName}");
                }
                
                squadElement.ImportantStatistic.SetText(set.Next.Connection, totalSquadXPWithHeader);
            }
        }
    }

    private void SendGlobalStats(LanguageSet set)
    {
        for (int index = 0; index < ValuablePlayers.Length; index++)
        {
            ValuablePlayer element = ValuablePlayers[index];
            if (index >= _globalStats!.Count)
            {
                while (set.MoveNext())
                {
                    element.Root.Hide(set.Next.Connection);
                }
                set.Reset();
                
                continue;
            }
            
            PlayerGlobalStat stat = _globalStats[index];
            string formattedStatValue = string.Format(set.Culture, stat.ValueFormatString, stat.StatValue);
            
            while (set.MoveNext())
            {
                ITransportConnection c = set.Next.Connection;
                element.Name.SetText(c, set.Next.Equals(stat.Player)
                    ? TranslationFormattingUtility.Colorize(stat.Player.Names.CharacterName, new Color32(204, 255, 212, 255))
                    : stat.Player.Names.CharacterName);
                element.Avatar.SetImage(c, stat.Player.SteamSummary.AvatarUrlSmall);
                element.Role.SetText(c, stat.StatHeader);
                element.Value.SetText(c, formattedStatValue);
                element.Root.Show(c);
            }
            set.Reset();
        }
    }
    
    // make sure to only call this after _sets is initialized!
    private List<PlayerGlobalStat> ComputeGlobalStats()
    {
        PlayerGlobalStat mvp = GetPlayerWithHighestStat(KnownStatNames.XP, "MVP", "{0:F0} XP");
        PlayerGlobalStat bestTeammate = GetPlayerWithHighestStat(KnownStatNames.Reputation, "Medal of Honor", "{0:F0} Rep");
        PlayerGlobalStat mostKills = GetPlayerWithHighestStat(KnownStatNames.Kills, "Most Deadly", "{0:F0} Kills");
        PlayerGlobalStat mostDeaths = GetPlayerWithHighestStat(KnownStatNames.Deaths, "Unluckiest", "{0:F0} Deaths");
        PlayerGlobalStat longestShot = GetPlayerWithHighestStat(
            player => player.Player.Component<PlayerGameStatsComponent>().LongestShot.SquaredDistance,
            "Sharpshooter", string.Empty /* special override later */);

        List<PlayerGlobalStat> stats = [ mvp, bestTeammate, mostKills, mostDeaths ];

        if (longestShot.StatValue > 100)
        {
            LongestShot shot = longestShot.Player.Component<PlayerGameStatsComponent>().LongestShot;
            longestShot.ValueFormatString = shot.ToString();
            stats.Add(longestShot);
        }

        WarfarePlayer? smelliest = _playerService.GetOnlinePlayerOrNull(76561198839009178);
        if (smelliest != null && RandomUtility.GetFloat(0, 1) < 0.1f) // 10% chance of displaying if online
            stats.Insert(2, new PlayerGlobalStat(smelliest, RandomUtility.GetInteger(3, 24), "Smelliest Soldier", "{0} days without shower"));
        
        return stats;
    }

    private struct PlayerGlobalStat
    {
        public readonly WarfarePlayer Player;
        public readonly double StatValue;
        public readonly string StatHeader;
        public string ValueFormatString;
        public PlayerGlobalStat(WarfarePlayer player, double statValue, string statHeader, string valueFormatString)
        {
            Player = player;
            StatHeader = statHeader;
            StatValue = statValue;
            ValueFormatString = valueFormatString;
        }
    }

    private PlayerGlobalStat GetPlayerWithHighestStat(string statName, string statLabelText, string valueFormatString)
    {
        double globalStatValue = double.MinValue;
        LeaderboardPlayer globalHighest = null!;
        
        foreach (LeaderboardSet leaderboardSet in _sets!)
        {
            int statIndex = leaderboardSet.GetStatisticIndex(statName);
            
            LeaderboardPlayer localHighest = leaderboardSet.Players.Aggregate((p1, p2) =>
            {
                double p1Value = leaderboardSet.GetStatisticValue(statIndex, p1.Player.Steam64);
                double p2Value = leaderboardSet.GetStatisticValue(statIndex, p2.Player.Steam64);
            
                return p1Value > p2Value ? p1 : p2;
            });
            
            double localStatValue = leaderboardSet.GetStatisticValue(statIndex, localHighest.Player.Steam64);
            
            if (localStatValue <= globalStatValue)
                continue;

            globalStatValue = localStatValue;
            globalHighest = localHighest;
        }
        
        return new PlayerGlobalStat(globalHighest.Player, globalStatValue, statLabelText, valueFormatString);
    }

    private PlayerGlobalStat GetPlayerWithHighestStat(Func<LeaderboardPlayer, double> selector, string statLabelText, string valueFormatString)
    {
        double globalStatValue = double.MinValue;
        WarfarePlayer globalHighest = null!;
        
        foreach (LeaderboardSet leaderboardSet in _sets!)
        {
            LeaderboardPlayer localHighest = leaderboardSet.Players.Aggregate((p1, p2) => selector(p1) > selector(p2) ? p1 : p2);
            
            double localStatValue = selector(localHighest);
            
            if (localStatValue <= globalStatValue)
                continue;

            globalStatValue = localStatValue;
            globalHighest = localHighest.Player;
        }
        
        return new PlayerGlobalStat(globalHighest, globalStatValue, statLabelText, valueFormatString);
    }

    private void SendPointsSection(WarfarePlayer player)
    {
        Team team = player.Team;
        LeaderboardSet? set = _sets!.FirstOrDefault(x => x.Team == team);
        WarfareRank rank = _pointsService.GetRankFromExperience(player.CachedPoints.XP);
        if (set == null)
        {
            PointsCreditsGained.SetText(player, string.Empty);
            PointsExperienceGained.SetText(player, string.Empty);
            PointsDowngradeArrow.SetVisibility(player, false);
            PointsUpgradeArrow.SetVisibility(player, false);
            PointsCurrentRank.SetText(player, string.Empty);
            PointsNextRank.SetText(player, string.Empty);
            PointsProgressBar.SetVisibility(player, false);
            return;
        }

        PointsProgressBar.SetVisibility(player, true);
        PointsProgressBar.SetProgress(player.Connection, (float)rank.GetProgress(player.CachedPoints.XP));

        double deltaCredits = set.GetStatisticValue(KnownStatNames.Credits, player.Steam64);
        double deltaXP = set.GetStatisticValue(KnownStatNames.XP, player.Steam64);

        PointsCreditsGained.SetText(player, deltaCredits > 0
            ? $"+{deltaCredits.ToString("F0", player.Locale.CultureInfo)} {TranslationFormattingUtility.Colorize("C", _pointsService.CreditsColor)}"
            : $"{deltaCredits.ToString("F0", player.Locale.CultureInfo)} {TranslationFormattingUtility.Colorize("C", _pointsService.CreditsColor)}");
        PointsExperienceGained.SetText(player, deltaXP > 0
            ? $"+{deltaXP.ToString("F0", player.Locale.CultureInfo)} {TranslationFormattingUtility.Colorize("XP", _pointsService.ExperienceColor)}"
            : $"{deltaXP.ToString("F0", player.Locale.CultureInfo)} {TranslationFormattingUtility.Colorize("XP", _pointsService.ExperienceColor)}");

        PointsCurrentRank.SetText(player, rank.Name);
        PointsNextRank.SetText(player, rank.Next?.Name ?? string.Empty);

        WarfareRank startingRank = _pointsService.GetRankFromExperience(player.CachedPoints.XP - deltaXP);
        PointsDowngradeArrow.SetVisibility(player, startingRank.RankIndex > rank.RankIndex);
        PointsUpgradeArrow.SetVisibility(player, startingRank.RankIndex < rank.RankIndex);
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