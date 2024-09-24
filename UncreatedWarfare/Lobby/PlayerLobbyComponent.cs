using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;
using static Uncreated.Warfare.Lobby.LobbyZoneManager;

namespace Uncreated.Warfare.Lobby;
public class PlayerLobbyComponent : IPlayerComponent
{
    private LobbyZoneManager _lobbyManager;

    private int _joiningTeam = -1;
    private int _lookingTeam = -1;
    private int _closestTeam = -1;

    public WarfarePlayer Player { get; private set; }

    public bool IsJoining => _joiningTeam >= 0;
    public bool IsLooking => _lookingTeam >= 0;
    public bool IsClosest => _closestTeam >= 0;
    public ref FlagInfo JoiningTeam => ref _lobbyManager.TeamFlags[_joiningTeam];
    public ref FlagInfo LookingTeam => ref _lobbyManager.TeamFlags[_lookingTeam];
    public ref FlagInfo ClosestTeam => ref _lobbyManager.TeamFlags[_closestTeam];

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _lobbyManager = serviceProvider.GetRequiredService<LobbyZoneManager>();
    }
    
    public void UpdatePositionalData(int lookingTeam, int closestTeam)
    {
        _lookingTeam = lookingTeam;
        _closestTeam = closestTeam;
    }

    public void StartJoiningTeam(int team)
    {
        if (team == -1)
        {
            _joiningTeam = -1;
            // cancel coroutine
        }
        else
        {
            _joiningTeam = team;
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    public void EnterLobby()
    {
        
    }

    public void ExitLobby()
    {
        _lookingTeam = -1;
        _closestTeam = -1;
    }
}
