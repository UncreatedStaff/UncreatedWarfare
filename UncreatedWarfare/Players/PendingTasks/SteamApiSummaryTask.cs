using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Steam.Models;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Queries the Steam API to get a player's summary.
/// </summary>
[PlayerTask]
public class SteamApiSummaryTask : IPlayerPendingTask
{
    private readonly ISteamApiService _apiService;

    private PlayerSummary? _summary;
    private ulong[]? _friends;
    public SteamApiSummaryTask(ISteamApiService apiService)
    {
        _apiService = apiService;
    }

    async Task<bool> IPlayerPendingTask.RunAsync(PlayerPending e, CancellationToken token)
    {
        Task<PlayerSummary> summaryTask = _apiService.GetPlayerSummaryAsync(e.Steam64.m_SteamID, token);
        PlayerFriendsList friendsList = await _apiService.GetPlayerFriendsAsync(e.Steam64.m_SteamID, token);
        _summary = await summaryTask;

        friendsList.Friends.Sort((a, b) => a.FriendsSince.CompareTo(b.FriendsSince));

        _friends = new ulong[friendsList.Friends.Count];
        for (int i = 0; i < _friends.Length; ++i)
        {
            _friends[i] = friendsList.Friends[i].Steam64;
        }

        return _summary != null;
    }

    void IPlayerPendingTask.Apply(WarfarePlayer player)
    {
        player.SteamSummary = _summary!;
        player.SteamFriends = _friends!;
    }

    bool IPlayerPendingTask.CanReject => true;
}
