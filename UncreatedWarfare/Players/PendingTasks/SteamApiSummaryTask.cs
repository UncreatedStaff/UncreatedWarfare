using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Steam.Models;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Queries the Steam API to get a player's summary.
/// </summary>
public class SteamApiSummaryTask : IPlayerPendingTask
{
    private readonly SteamApiService _apiService;

    private PlayerSummary? _summary;
    private ulong[]? _friends;
    public SteamApiSummaryTask(SteamApiService apiService)
    {
        _apiService = apiService;
    }

    async Task<bool> IPlayerPendingTask.RunAsync(PlayerPending e, CancellationToken token)
    {
        UniTask<PlayerSummary?> summaryTask = _apiService.GetPlayerSummary(e.Steam64.m_SteamID, token);
        PlayerFriendsList friendsList = await _apiService.GetPlayerFriends(e.Steam64.m_SteamID, token);
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
