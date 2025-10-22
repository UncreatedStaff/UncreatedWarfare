using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Layouts.Seeding;

public class SeedingModePlayerVoteManager : IPlayerVoteManager
{
    public bool IsVoting => throw new NotImplementedException();

    public DateTime VoteStart => throw new NotImplementedException();

    public DateTime VoteEnd => throw new NotImplementedException();

    /// <inheritdoc />
    public UniTask StartVoteAsync(VoteSettings settings, Action<IVoteResult>? callback, CancellationToken startCancellationToken = default)
    {
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask EndVoteAsync(CancellationToken token = default, bool cancelled = false)
    {
        return UniTask.CompletedTask;
    }

    public void StartVote(in VoteSettings settings, Action<VoteResult> callback)
    {
        throw new NotImplementedException();
    }

    public PlayerVoteState GetVoteState(CSteamID player)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public int GetVoteCount(PlayerVoteState vote)
    {
        return 0;// todo
    }

    /// <inheritdoc />
    public PlayerVoteState RegisterVote(CSteamID player, PlayerVoteState vote)
    {
        return 0;// todo
    }
}