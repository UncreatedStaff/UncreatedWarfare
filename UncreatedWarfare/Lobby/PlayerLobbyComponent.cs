using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using StackCleaner;
using System;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Zones;
using static Uncreated.Warfare.Lobby.LobbyZoneManager;

namespace Uncreated.Warfare.Lobby;

[PlayerComponent]
public class PlayerLobbyComponent : IPlayerComponent
{
    private QuestAsset? _previouslyTrackedQuest;

#nullable disable
    private LobbyZoneManager _lobbyManager;
    private LobbyHudUI _ui;
    private WarfareModule _module;
    private IPlayerService _playerService;
    private bool _hasUi;
    private ITeamManager<Team> _teamManager;
    private ZoneStore _zoneStore;
    private ILogger<PlayerLobbyComponent> _logger;
    private SquadManager _squadManager;

    public WarfarePlayer Player { get; private set; }

#nullable restore

    private int _joiningTeam = -1;
    private int _lookingTeam = -1;
    private int _closestTeam = -1;


    public bool IsJoining => _joiningTeam >= 0;
    public bool IsLooking => _lookingTeam >= 0;
    public bool IsClosest => _closestTeam >= 0;
    public ref FlagInfo JoiningTeam => ref _lobbyManager.TeamFlags![_joiningTeam];
    public ref FlagInfo LookingTeam => ref _lobbyManager.TeamFlags![_lookingTeam];
    public ref FlagInfo ClosestTeam => ref _lobbyManager.TeamFlags![_closestTeam];

    public bool IsInLobby { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _lobbyManager = serviceProvider.GetRequiredService<LobbyZoneManager>();
        _ui = serviceProvider.GetRequiredService<LobbyHudUI>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _teamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _logger = serviceProvider.GetRequiredService<ILogger<PlayerLobbyComponent>>();
        _squadManager = serviceProvider.GetRequiredService<SquadManager>();
    }
    
    public void UpdatePositionalData(int lookingTeam, int closestTeam)
    {
        _closestTeam = closestTeam;

        if (_lookingTeam == lookingTeam)
            return;
        
        _lookingTeam = lookingTeam;

        if (_hasUi)
            UpdateUI();
    }

    public void StartJoiningTeam(int team)
    {
        if (team == -1)
        {
            _joiningTeam = -1;
            return;
        }

        _joiningTeam = team;
        _ = JoinTeamIntl(team);
    }
    
    private async UniTask JoinTeamIntl(int teamIndex)
    {
        if (_joiningTeam != teamIndex || _lobbyManager.TeamFlags == null)
            return;

        bool needsUpdate = false;
        if (_lobbyManager.JoinDelay > TimeSpan.Zero)
        {
            _lobbyManager.UpdateTeamCount(teamIndex, +1);
            needsUpdate = true;
            try
            {
                await UniTask.Delay(_lobbyManager.JoinDelay, DelayType.Realtime, cancellationToken: Player.DisconnectToken);
            }
            catch
            {
                _lobbyManager.UpdateTeamCount(teamIndex, -1);
                throw;
            }
        }

        Team joiningTeam;
        try
        {
            if (_joiningTeam != teamIndex)
                return;

            joiningTeam = _joiningTeam < 0 || _joiningTeam >= _lobbyManager.TeamFlags.Length ? Team.NoTeam : (_lobbyManager.TeamFlags[_joiningTeam].Team ?? Team.NoTeam);

            // join Unturned group
            UniTask joinTeamTask = _teamManager.JoinTeamAsync(Player, joiningTeam, Player.DisconnectToken);
            _joiningTeam = -1;
            await joinTeamTask;
        }
        finally
        {
            if (needsUpdate)
                _lobbyManager.UpdateTeamCount(teamIndex, -1);
        }

        if (!joiningTeam.IsValid)
            return;

        // teleport to main base
        await UniTask.SwitchToMainThread(Player.DisconnectToken);
        Zone? mainBase = _zoneStore.SearchZone(ZoneType.MainBase, joiningTeam.Faction);

        if (mainBase == null)
        {
            _logger.LogWarning("Unable to find main base to teleport player {0} to for team {1}.", Player, joiningTeam.Faction.Name);
        }
        else
        {
            Player.UnturnedPlayer.teleportToLocationUnsafe(mainBase.Spawn, mainBase.SpawnYaw);
        }
    }

    public void EnterLobby()
    {
        _previouslyTrackedQuest = Player.UnturnedPlayer.quests.GetTrackedQuest();
        if (_previouslyTrackedQuest != null)
        {
            QuestService.ServerUntrackQuest(Player, _previouslyTrackedQuest);
        }

        IsInLobby = true;
        UpdateUI(send: true);
    }

    public void ExitLobby()
    {
        _ui.ClearFromPlayer(Player.Connection);
        _hasUi = false;
        _lookingTeam = -1;
        _closestTeam = -1;
        IsInLobby = false;

        if (_previouslyTrackedQuest != null)
        {
            QuestService.ServerTrackQuest(Player, _previouslyTrackedQuest);
        }
    }

    /// <summary>
    /// Fully updates the UI.
    /// </summary>
    public void UpdateUI(bool send = false)
    {
        if (send)
            _hasUi = false;

        Layout? layout = _module.IsLayoutActive() ? _module.GetActiveLayout() : null;
        string layoutName = layout?.LayoutInfo.DisplayName.ToUpper(Player.Locale.CultureInfo) ?? string.Empty;

        ITransportConnection connection = Player.Connection;
        if (_lookingTeam == -1)
        {
            if (send)
                _ui.SendToPlayer(connection, string.Empty, layoutName, string.Empty);
            else
            {
                _ui.FactionName.SetText(connection, string.Empty);
                _ui.FactionInfo[0].SetText(connection, layoutName);
                _ui.FactionInfo[1].SetText(connection, string.Empty);
                _ui.ResetInfo(connection);
            }
            return;
        }

        string playerCountString;
        if (layout?.TeamManager is TwoSidedTeamManager { HasBothTeams: true } twoSided)
        {
            bool atk = twoSided.Opfor == LookingTeam.Team;

            // ATTACKING - 22/48 todo translations
            playerCountString = (atk ? "ATTACKING - " : "DEFENDING - ")
                                       + _lobbyManager.GetTeamPlayerCount(_lookingTeam).ToString(Player.Locale.CultureInfo)
                                       + "/"
                                       + _lobbyManager.GetActivePlayerCount().ToString(Player.Locale.CultureInfo);
        }
        else
        {
            // 22/48
            playerCountString = _lobbyManager.GetTeamPlayerCount(_lookingTeam).ToString(Player.Locale.CultureInfo)
                                       + "/"
                                       + _lobbyManager.GetActivePlayerCount().ToString(Player.Locale.CultureInfo);
        }

        string factionName = LookingTeam.Faction.NameTranslations.Translate(Player.Locale.LanguageInfo) ?? LookingTeam.Faction.Name;
        factionName = TranslationFormattingUtility.Colorize(factionName, LookingTeam.Faction.Color, TranslationOptions.TMProUI, StackColorFormatType.None);

        if (send)
        {
            _ui.SendToPlayer(connection, factionName, layoutName, playerCountString);
        }
        else
        {
            _ui.FactionName.SetText(connection, factionName);
            _ui.FactionInfo[0].SetText(connection, layoutName);
            _ui.FactionInfo[1].SetText(connection, playerCountString);
        }

        UpdateTeamInfo();
        _hasUi = true;
    }

    /// <summary>
    /// Updates squad leader and friend list.
    /// </summary>
    public void UpdateTeamInfo()
    {
        ITransportConnection connection = Player.Connection;

        if (_hasUi)
            _ui.ResetInfo(connection);

        int index = 2;
        bool hasSetSquadTitle = false, hasSetFriendTitle = false;

        // shows a list of all squad leaders
        // todo add squad stuff, order by letter
        foreach (Squad squad in _squadManager.Squads)
        {
            if (squad.Team != LookingTeam.Team)
                continue;

            if (!hasSetSquadTitle)
            {
                // todo translations
                UnturnedLabel lbl = _ui.FactionInfo[index];
                lbl.SetText(connection, "SQUAD LEADERS");
                lbl.SetVisibility(connection, true);
                ++index;
                hasSetSquadTitle = true;
            }

            UnturnedLabel lblName = _ui.FactionInfo[index];
            lblName.SetText(connection, squad.Leader.Names.CharacterName);
            lblName.SetVisibility(connection, true);
            ++index;
        }

        // shows a list of all steam friends that are currently on the team in order of how long they've been friends
        ulong[] friends = Player.SteamFriends;

        foreach (ulong steam64Id in friends)
        {
            WarfarePlayer? onlineFriend = _playerService.GetOnlinePlayerOrNull(steam64Id);
            if (onlineFriend == null || onlineFriend.Team != LookingTeam.Team)
                continue;

            if (!hasSetFriendTitle)
            {
                // todo translations
                UnturnedLabel lbl = _ui.FactionInfo[index];
                lbl.SetText(connection, "FRIENDS IN TEAM");
                lbl.SetVisibility(connection, true);
                ++index;
                hasSetFriendTitle = true;
            }

            UnturnedLabel lblName = _ui.FactionInfo[index];
            lblName.SetText(connection, onlineFriend.Names.PlayerName + " |");
            lblName.SetVisibility(connection, true);
            ++index;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    public bool TryTrackQuest(QuestAsset quest)
    {
        if (IsInLobby)
        {
            _previouslyTrackedQuest = quest;
            return false;
        }

        return true;
    }
}